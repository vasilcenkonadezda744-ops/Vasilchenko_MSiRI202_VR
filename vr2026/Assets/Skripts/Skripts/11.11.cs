using System;
using System.Collections;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class OIU : MonoBehaviour
{
    [Header("Настройки сети")]
    public string url = "https://cnc.kovalev.team/get/3"; 
    public string commandUrl = "https://cnc.kovalev.team/post"; 
    public int channel = 1;
    public float interval = 0.5f;

    [Header("Узлы станка")]
    public Transform nodeX;
    public Transform nodeY;
    public Transform nodeZ;

    [Header("Тексты на панели")]
    public TMP_Text axesText;       
    public TMP_Text connectionText; 

    public bool IsConnected { get; private set; } = false;

    private Vector3 startX, startY, startZ;
    private bool hasData = false;

    private IEnumerator Start()
    {
        if (nodeX == null || nodeY == null || nodeZ == null)
        {
            Debug.LogError("Назначьте узлы станка в Inspector!");
            yield break;
        }

        startX = nodeX.localPosition;
        startY = nodeY.localPosition;
        startZ = nodeZ.localPosition;

        if (axesText != null) axesText.text = "X: - мм\nY: - мм\nZ: - мм";
        if (connectionText != null) connectionText.text = "Ожидание...";

        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    IsConnected = true;
                    try
                    {
                        JObject data = JObject.Parse(request.downloadHandler.text);
                        float x = GetCoordinate(data, "X") * 0.001f;
                        float y = GetCoordinate(data, "Y") * 0.001f;
                        float z = GetCoordinate(data, "Z") * 0.001f;

                        nodeX.localPosition = startX + Vector3.right * x;
                        nodeY.localPosition = startY + Vector3.forward * y;
                        nodeZ.localPosition = startZ + Vector3.up * z;

                        if (axesText != null)
                            axesText.text = $"Канал {channel}\nX: {x * 1000f:F1} мм\nY: {y * 1000f:F1} мм\nZ: {z * 1000f:F1} мм";

                        if (connectionText != null) connectionText.text = "Данные получены";
                        hasData = true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Ошибка JSON: " + e.Message);
                        if (connectionText != null) connectionText.text = "Ошибка данных";
                    }
                }
                else
                {
                    IsConnected = false;
                    if (connectionText != null) 
                        connectionText.text = "Нет связи" + (hasData ? " (устарело)" : "");
                }
            }
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    public void StartMachine()
    {
        StartCoroutine(SendCommand("start"));
    }

    public void StopMachine()
    {
        StartCoroutine(SendCommand("stop"));
    }

    private IEnumerator SendCommand(string command)
    {
        string jsonBody = "{\"command\":\"" + command + "\"}";
        
        using (UnityWebRequest request = new UnityWebRequest(commandUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка команды: " + request.error);
            }
            else
            {
                Debug.Log("POST успешен: " + request.downloadHandler.text);
            }
        }
    }

    private float GetCoordinate(JObject data, string axis)
    {
        foreach (JToken section in data["data"])
        {
            if ((string)section[0] != "Оси Канала " + channel) continue;

            foreach (JToken row in section[1])
            {
                if ((string)row[0] == "Перемещение по оси " + axis)
                {
                    string text = ((string)row[1]).Replace("мм", "").Trim().Replace(',', '.');
                    return float.Parse(text, CultureInfo.InvariantCulture);
                }
            }
        }
        throw new FormatException("Не найдена координата " + axis);
    }
}