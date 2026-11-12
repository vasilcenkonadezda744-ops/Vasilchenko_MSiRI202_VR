using System.Collections;
using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;

/// <summary>
/// Центральный контроллер тренажёра 3D-принтера.
/// </summary>
public class MachineTrainer : MonoBehaviour
{
    [Header("=== Заготовка ===")]
    public Transform TrainingBlank;
    public Renderer BlankRenderer;
    public Material RawMaterial;
    public Material FinishedMaterial;

    [Header("=== Точки и зоны ===")]
    public Transform BlankHome;
    public Transform WorkPoint;
    public Transform OutputPoint;
    public BoxCollider WorkZone;
    public BoxCollider OutputZone;

    [Header("=== Подвижный узел (экструдер) ===")]
    public Transform MovingNode;
    public Vector3 MoveDirection = Vector3.right;
    public float Travel = 0.12f;
    public float Duration = 5f;

    [Header("=== UI (World Space TMP) ===")]
    public TMP_Text StageText;
    public TMP_Text HintText;
    public TMP_Text StatusText;

    [Header("=== Интеграция с реальным станком (JSonData) ===")]
    [Tooltip("Скрипт, который качает данные с сервера.")]
    public JSonData RealMachineData; // ИЗМЕНЕНО: Теперь ссылаемся на JSonData

    // ---- Внутреннее состояние ----
    private enum TrainingStage
    {
        Introduction, CheckPrinter, InstallBlank, ReadyToPrint, Printing, Completed, Delivering
    }

    private TrainingStage _stage = TrainingStage.Introduction;
    private bool _isPrinting = false;
    private bool _blankIsReady = false;
    private bool _blankSnappedInWork = false;
    private bool _blankSnappedInOutput = false;

    private Vector3 _movingNodeStartPos;
    private Coroutine _printCoroutine;

    private TrainingBlank _blankScript;
    private Rigidbody _blankRigidbody;

    private void Awake()
    {
        if (TrainingBlank != null)
        {
            _blankScript = TrainingBlank.GetComponent<TrainingBlank>();
            _blankRigidbody = TrainingBlank.GetComponent<Rigidbody>();
        }

        if (MovingNode != null)
            _movingNodeStartPos = MovingNode.localPosition;

        ResetBlankToHome();
    }

    private void Start()
    {
        UpdateAllUI();
    }

    public void NextStep()
    {
        if (_isPrinting) { ShowHint("Нельзя менять этап во время печати."); return; }

        switch (_stage)
        {
            case TrainingStage.Introduction:
                _stage = TrainingStage.CheckPrinter;
                ShowHint("Проверьте, что принтер чист. Нажмите «Далее».");
                break;
            case TrainingStage.CheckPrinter:
                _stage = TrainingStage.InstallBlank;
                ShowHint("Возьмите заготовку и поместите её в рабочую зону (WorkZone).");
                break;
            case TrainingStage.InstallBlank:
                if (!_blankSnappedInWork) { ShowHint("Сначала установите заготовку в WorkZone."); return; }
                _stage = TrainingStage.ReadyToPrint;
                ShowHint("Заготовка установлена. Нажмите «Пуск».");
                break;
            default:
                ShowHint("Сейчас не требуется переключение этапа.");
                return;
        }
        UpdateAllUI();
    }

    public void StartCycle()
    {
        if (_isPrinting) { ShowHint("Печать уже идёт!"); return; }
        if (_stage != TrainingStage.ReadyToPrint) { ShowHint("Пройдите предыдущие этапы."); return; }
        if (!_blankSnappedInWork) { ShowHint("Заготовка не установлена в рабочей зоне."); return; }
        if (_blankScript != null && _blankScript.IsHeld) { ShowHint("Отпустите заготовку перед запуском."); return; }
        if (_blankIsReady) { ShowHint("Заготовка уже обработана."); return; }

        if (_blankScript != null) _blankScript.SetGrabBlocked(true);

        // Если есть скрипт JSonData, просто пишем в лог, что начинаем (сам станок ездит от данных сервера)
        if (RealMachineData != null)
        {
            Debug.Log("[MachineTrainer] Запуск цикла (данные с сервера).");
        }

        _printCoroutine = StartCoroutine(PrintCycleRoutine());
    }

    public void StopCycle()
    {
        if (!_isPrinting) { ShowHint("Печать не запущена."); return; }

        if (_printCoroutine != null) { StopCoroutine(_printCoroutine); _printCoroutine = null; }

        _isPrinting = false;
        if (MovingNode != null) MovingNode.localPosition = _movingNodeStartPos;
        if (_blankScript != null) _blankScript.SetGrabBlocked(false);

        _stage = TrainingStage.ReadyToPrint;
        ShowStatus("Печать прервана.");
        ShowHint("Возврат в исходное положение.");
        UpdateAllUI();
    }

    public void RestartTraining()
    {
        if (_blankScript != null && _blankScript.IsHeld)
        {
            ShowHint("Отпустите деталь перед сбросом!");
            return;
        }

        if (_printCoroutine != null) { StopCoroutine(_printCoroutine); _printCoroutine = null; }

        _isPrinting = false;
        _blankIsReady = false;
        _blankSnappedInWork = false;
        _blankSnappedInOutput = false;
        _stage = TrainingStage.Introduction;

        if (MovingNode != null) MovingNode.localPosition = _movingNodeStartPos;
        if (_blankScript != null) _blankScript.SetGrabBlocked(false);
        if (BlankRenderer != null && RawMaterial != null) BlankRenderer.material = RawMaterial;

        ResetBlankToHome();

        if (StatusText != null) StatusText.text = "";
        ShowHint("Тренажёр сброшен.");
        UpdateAllUI();
    }

    public void OnBlankReleased()
    {
        if (TrainingBlank == null) return;
        if (_blankSnappedInOutput) return;

        Vector3 blankPos = TrainingBlank.position;

        if (WorkZone != null && WorkZone.bounds.Contains(blankPos))
        {
            if (_blankIsReady) { ShowHint("Деталь уже готова. Перенесите её в OutputZone."); return; }
            
            SnapBlankTo(WorkPoint);
            _blankSnappedInWork = true;
            _blankSnappedInOutput = false;

            if (_stage == TrainingStage.InstallBlank)
            {
                _stage = TrainingStage.ReadyToPrint;
                ShowHint("Заготовка зафиксирована. Нажмите «Пуск».");
            }
            UpdateAllUI();
            return;
        }

        if (OutputZone != null && OutputZone.bounds.Contains(blankPos))
        {
            if (!_blankIsReady) { ShowHint("Нельзя сдать необработанную заготовку!"); return; }
            
            SnapBlankTo(OutputPoint);
            _blankSnappedInOutput = true;
            _blankSnappedInWork = false;
            _stage = TrainingStage.Completed;
            ShowStatus("Готово! Деталь сдана.");
            ShowHint("Цикл завершён. Нажмите «Повторить».");
            
            if (_blankScript != null) _blankScript.SetGrabBlocked(true);
            UpdateAllUI();
            return;
        }
    }

    private IEnumerator PrintCycleRoutine()
    {
        _isPrinting = true;
        _stage = TrainingStage.Printing;

        ShowStatus("Печать...");
        ShowHint("Идёт печать. Не трогайте заготовку.");
        UpdateAllUI();

        // Ждем окончания печати (локально)
        yield return new WaitForSeconds(Duration);

        _isPrinting = false;
        _printCoroutine = null;
        _blankIsReady = true;

        if (BlankRenderer != null && FinishedMaterial != null)
            BlankRenderer.material = FinishedMaterial;

        if (_blankScript != null)
            _blankScript.SetGrabBlocked(false);

        _stage = TrainingStage.Delivering;

        ShowStatus("Печать завершена.");
        ShowHint("Возьмите готовую деталь и перенесите её в OutputZone.");
        UpdateAllUI();
    }

    private void SnapBlankTo(Transform targetPoint)
    {
        if (TrainingBlank == null || targetPoint == null) return;
        TrainingBlank.position = targetPoint.position;
        TrainingBlank.rotation = targetPoint.rotation;

        if (_blankRigidbody != null)
        {
            _blankRigidbody.linearVelocity = Vector3.zero;
            _blankRigidbody.angularVelocity = Vector3.zero;
            _blankRigidbody.isKinematic = true;
        }
    }

    private void ResetBlankToHome()
    {
        if (TrainingBlank == null || BlankHome == null) return;
        TrainingBlank.position = BlankHome.position;
        TrainingBlank.rotation = BlankHome.rotation;

        if (_blankRigidbody != null)
        {
            _blankRigidbody.linearVelocity = Vector3.zero;
            _blankRigidbody.angularVelocity = Vector3.zero;
            _blankRigidbody.isKinematic = false;
        }

        if (BlankRenderer != null && RawMaterial != null)
            BlankRenderer.material = RawMaterial;

        _blankSnappedInWork = false;
        _blankSnappedInOutput = false;
        _blankIsReady = false;
    }

    private void ShowHint(string text) { if (HintText != null) HintText.text = text; }
    private void ShowStatus(string text) { if (StatusText != null) StatusText.text = text; }

    private void UpdateAllUI()
    {
        if (StageText != null)
        {
            switch (_stage)
            {
                case TrainingStage.Introduction: StageText.text = "Этап 1: Ознакомление"; break;
                case TrainingStage.CheckPrinter: StageText.text = "Этап 2: Проверка"; break;
                case TrainingStage.InstallBlank: StageText.text = "Этап 3: Установка"; break;
                case TrainingStage.ReadyToPrint: StageText.text = "Этап 4: Готовность"; break;
                case TrainingStage.Printing: StageText.text = "Этап 5: Печать..."; break;
                case TrainingStage.Delivering: StageText.text = "Этап 6: Сдача"; break;
                case TrainingStage.Completed: StageText.text = "Этап 7: Завершено"; break;
            }
        }

        if (StatusText != null && string.IsNullOrEmpty(StatusText.text))
        {
            if (_isPrinting) StatusText.text = "Статус: печать идёт";
            else if (_blankIsReady && !_blankSnappedInOutput) StatusText.text = "Статус: деталь готова";
            else if (_blankSnappedInOutput) StatusText.text = "Статус: выполнено";
            else StatusText.text = "Статус: ожидание";
        }
    }
}