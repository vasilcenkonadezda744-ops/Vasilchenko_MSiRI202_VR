using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BallSpawner : MonoBehaviour
{
    [Header("Детали принтера")]
    public GameObject blackBallPrefab;
    public Transform printBed;
    public Transform nozzle;
    public ParticleSystem plasticStream;

    [Header("Текст статуса и счётчика на белой консоли")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI counterText;

    [Header("Размеры и положение")]
    public float ballSize = 1.0f;
    public float heightAboveBed = 0.05f;

    [Header("Настройки печати и выброса")]
    public float printTime = 2.0f;
    public float nozzleSpeed = 18.0f;
    public float nozzleAmplitude = 0.08f;
    public float delayBeforeEject = 0.2f;
    public Vector3 ejectDirection = new Vector3(1f, 0.4f, 0f);
    public float ejectForce = 3.5f;
    public int maxBallsOnScene = 30;

    private bool isRunning = false;
    private int printedCount = 0;
    private Vector3 nozzleStartPos;
    private Queue<GameObject> spawnedBalls = new Queue<GameObject>();

    private void Start()
    {
        if (nozzle != null)
            nozzleStartPos = nozzle.localPosition;

        UpdateCounterUI();
    }

    public void StartPrinting()
    {
        if (!isRunning)
        {
            isRunning = true;
            if (statusText != null) statusText.text = "Старт печати";
            Debug.Log("BallSpawner: СТАРТ печати");
            StartCoroutine(EndlessPrintLoop());
        }
    }

    public void StopPrinting()
    {
        isRunning = false;
        StopAllCoroutines();
        if (plasticStream != null) plasticStream.Stop();
        if (nozzle != null) nozzle.localPosition = nozzleStartPos;
        if (statusText != null) statusText.text = "Ожидание...";
        Debug.Log("BallSpawner: СТОП печати");
    }

    private void UpdateCounterUI()
    {
        if (counterText != null)
            counterText.text = "Напечатано деталей: " + printedCount;
    }

    private IEnumerator EndlessPrintLoop()
    {
        while (isRunning)
        {
            if (blackBallPrefab == null || printBed == null)
            {
                isRunning = false;
                yield break;
            }

            if (spawnedBalls.Count >= maxBallsOnScene)
            {
                GameObject oldestBall = spawnedBalls.Dequeue();
                if (oldestBall != null) Destroy(oldestBall);
            }

            Vector3 spawnPos = printBed.position + Vector3.up * heightAboveBed;
            GameObject ball = Instantiate(blackBallPrefab, spawnPos, Quaternion.identity);
            spawnedBalls.Enqueue(ball);

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            if (plasticStream != null) plasticStream.Play();

            Vector3 startScale = Vector3.one * 0.01f;
            Vector3 targetScale = Vector3.one * ballSize;
            float elapsed = 0f;

            while (elapsed < printTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / printTime);

                ball.transform.position = printBed.position + Vector3.up * heightAboveBed;
                ball.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                if (nozzle != null)
                {
                    float offsetX = Mathf.Sin(elapsed * nozzleSpeed) * nozzleAmplitude;
                    float offsetZ = Mathf.Cos(elapsed * nozzleSpeed * 0.8f) * nozzleAmplitude;
                    nozzle.localPosition = nozzleStartPos + new Vector3(offsetX, 0f, offsetZ);
                }

                yield return null;
            }

            ball.transform.localScale = targetScale;
            if (plasticStream != null) plasticStream.Stop();
            if (nozzle != null) nozzle.localPosition = nozzleStartPos;

            printedCount++;
            UpdateCounterUI();

            yield return new WaitForSeconds(delayBeforeEject);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(ejectDirection.normalized * ejectForce, ForceMode.Impulse);
            }

            yield return new WaitForSeconds(0.3f);
        }
    }
}