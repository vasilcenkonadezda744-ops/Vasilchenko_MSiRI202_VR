using TMPro;
using UnityEngine;

public class Panel1 : MonoBehaviour
{
    public TMP_Text axes, conn, cmd;
    private bool hasData;
    private float lastSuccess;
    private string val = "X: - мм\nY: - мм\nZ: - мм";

    void Awake()
    {
        // Проверка, видит ли скрипт текст
        Debug.Log("Panel1: Axes найден? " + (axes != null));

        if (axes != null) axes.text = "ТЕСТ (Awake)"; 
        if (conn != null) conn.text = "Ожидание..."; 
        if (cmd != null) cmd.text = "Нет команд";
    }

    public void ShowCoord(float x, float y, float z, int ch)
    {
        // Проверка, доходят ли данные
        Debug.Log("Panel1: Получены координаты: " + x + ", " + y + ", " + z);

        val = $"Канал {ch}\nX: {x * 1000f:F1} мм\nY: {y * 1000f:F1} мм\nZ: {z * 1000f:F1} мм";
        
        if (axes != null) axes.text = val; 
        if (conn != null) conn.text = "Данные есть";
        
        hasData = true; 
        lastSuccess = Time.unscaledTime;
    }

    public void ShowErr(string msg)
    {
        if (conn != null) 
            conn.text = msg + (hasData ? " (устарело)" : " (нет данных)");
    }

    public void ShowCmd(string msg) 
    { 
        if (cmd != null) cmd.text = msg; 
    }

    void Update()
    {
        if (hasData && Time.unscaledTime - lastSuccess > 3f) 
            ShowErr("Нет свежих данных");
    }
}