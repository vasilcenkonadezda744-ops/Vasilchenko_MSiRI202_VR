using System;
using System.Collections;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class MachineController : MonoBehaviour
{
    [Header("Настройки сети")]
    public string url = "https://cnc.kovalev.team/get/3";
    public int channel = 1;
    public float interval = 1f;

    [Header("Объекты станка")]
    [Tooltip("Шпиндель (двигается влево-вправо)")]
    public Transform nodeX;
    [Tooltip("Стол (двигается вверх-вниз)")]
    public Transform nodeY; 
    [Tooltip("Портал/салазки (двигается вперед-назад)")]
    public Transform nodeZ;

    [Header("Масштаб и направление")]
    [Tooltip("Коэффициент перевода мм в единицы Unity. 0.01 = 1мм -> 0.01 юнита")]
    public float scaleFactor = 0.01f;
    
    [Tooltip("Инвертировать ось Z (если стол едет не туда)")]
    public bool invertZ = false;

    private Vector3 startX;
    private Vector3 startY;
    private Vector3 startZ;

    private IEnumerator Start()
    {
        if (nodeX == null || nodeY == null || nodeZ == null)
        {
            Debug.LogError("Назначьте все три узла в Inspector.");
            yield break;
        }

        // Запоминаем начальные позиции
        startX = nodeX.localPosition;
        startY = nodeY.localPosition;
        startZ = nodeZ.localPosition;

        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        MoveNodes(request.downloadHandler.text);
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Ошибка чтения JSON: " + error.Message);
                    }
                }
                else
                {
                    Debug.LogWarning("Ошибка запроса: " + request.error);
                }
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, interval));
        }
    }

    private float GetCoordinate(JObject data, string axis)
    {
        foreach (JToken section in data["data"])
        {
            if ((string)section[0] != "Оси Канала " + channel)
                continue;

            foreach (JToken row in section[1])
            {
                if ((string)row[0] == "Перемещение по оси " + axis)
                {
                    string text = ((string)row[1]).Replace("мм", "").Trim();
                    text = text.Replace(',', '.');
                    float value = float.Parse(text, CultureInfo.InvariantCulture);

                    if (float.IsNaN(value) || float.IsInfinity(value))
                        throw new FormatException("Некорректная координата " + axis);

                    return value;
                }
            }
        }
        throw new FormatException("Не найдена координата " + axis);
    }

    private void MoveNodes(string json)
    {
        JObject data = JObject.Parse(json);
        
        // Получаем значения в мм и переводим в юниты
        float x = GetCoordinate(data, "X") * scaleFactor;
        float y = GetCoordinate(data, "Y") * scaleFactor;
        float z = GetCoordinate(data, "Z") * scaleFactor;

        // 1. Шпиндель: влево-вправо (Ось X)
        nodeX.localPosition = startX + Vector3.right * x;

        // 2. Стол: вверх-вниз (Ось Z станка = Vector3.up в Unity)
        float zDirection = invertZ ? -1f : 1f;
        nodeY.localPosition = startY + Vector3.up * (z * zDirection);

        // 3. Портал/салазки: вперед-назад (Ось Y)
        nodeZ.localPosition = startZ + Vector3.forward * y;
    }
}
