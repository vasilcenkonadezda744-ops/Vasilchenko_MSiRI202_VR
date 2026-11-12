using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class VRMachineController : MonoBehaviour
{
    [Header("Ссылки на UI панели")]
    public TMP_Text stateText;
    public TMP_Text axesText;
    public TMP_Text messageText;

    [Header("Настройки сети")]
    public string url = "https://cnc.kovalev.team/get/3"; 
    public int channel = 1;
    public float interval = 1f;
    public string postUrl = "https://cnc.kovalev.team/post/3"; 

    private void Start()
    {
        // Автоматическое обновление данных каждые interval секунд
        InvokeRepeating(nameof(FetchState), 1f, interval);
    }

    // Методы для кнопок On Click
    public void StartMachine() { StartCoroutine(SendCommand("start")); }
    public void StopMachine() { StartCoroutine(SendCommand("stop")); }

    // --- GET ЗАПРОС (ПОЛУЧЕНИЕ ДАННЫХ) ---
    public void FetchState()
    {
        StartCoroutine(GetStateCoroutine());
    }

    private IEnumerator GetStateCoroutine()
    {
        string getRequestUrl = $"{url}?channel={channel}";

        using (UnityWebRequest req = UnityWebRequest.Get(getRequestUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                ShowMessage("Нет связи");
            }
            else
            {
                string json = req.downloadHandler.text;
                try 
                {
                    var data = JsonUtility.FromJson<MachineState>(json);
                    ShowState(data); 
                    ShowMessage("Связь установлена"); 
                }
                catch (Exception e)
                {
                    Debug.LogError("Ошибка JSON: " + e.Message);
                    ShowMessage("Ошибка данных");
                }
            }
        }
    }

    // --- POST ЗАПРОС (ОТПРАВКА КОМАНД) ---
    private IEnumerator SendCommand(string command)
    {
        ShowMessage("Отправка команды...");
        string jsonPayload = "{\"command\":\"" + command + "\", \"channel\":" + channel + "}";
        
        using (UnityWebRequest req = new UnityWebRequest(postUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                ShowMessage("Ошибка: " + req.error);
                yield break;
            }

            ShowMessage("Команда принята...");
            FetchState(); // Сразу обновляем данные после команды
        }
    }

    // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
    private void ShowState(MachineState data)
    {
        stateText.text = data.running ? "Работает" : "Остановлен";
        axesText.text = $"X: {data.x:F1} mm\nY: {data.y:F1} mm\nZ: {data.z:F1} mm";
    }

    private void ShowMessage(string message)
    {
        messageText.text = message;
    }
}

// Класс для чтения JSON
[Serializable]
public class MachineState
{
    public bool running;
    public float x;
    public float y;
    public float z;
}