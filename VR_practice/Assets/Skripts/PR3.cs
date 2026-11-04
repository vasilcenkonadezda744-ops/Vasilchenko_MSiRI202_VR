using UnityEngine;
using System.Collections;

public class CubeManager : MonoBehaviour
{
      public string[] figures = { "Cube_1", "Cube_2", "Cube_3" };
    
    [Tooltip("Если включено - кубы будут проходить сквозь друг друга (Триггеры). Если выключено - будут отскакивать (Коллизии).")]
    public bool useTriggers = false; 

    private bool isGenerated = false;
    private bool hasBounced = false;

    void Start()
    {
       
        if (isGenerated) return;

        for (int i = 0; i < figures.Length; i++)
        {
            // 1. Создание фигуры
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = figures[i];
            cube.transform.position = new Vector3((i - 1) * 4f, 1f, 0);

                       BoxCollider collider = cube.GetComponent<BoxCollider>();
            collider.isTrigger = useTriggers; // Включаем/выключаем триггер

            // 2. Добавление физики
            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | 
                             RigidbodyConstraints.FreezePositionZ | 
                             RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Начальное движение
            if (i == 0) rb.linearVelocity = Vector3.right * 3f;
            else if (i == 2) rb.linearVelocity = Vector3.left * 3f;

                     CubeManager handler = cube.AddComponent<CubeManager>();
            handler.isGenerated = true;
            handler.useTriggers = this.useTriggers; // Передаем настройку
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (useTriggers || !isGenerated || hasBounced) return;

        if (collision.gameObject.GetComponent<CubeManager>() != null)
        {
            Debug.Log($"[Коллизия] {gameObject.name} столкнулся с {collision.gameObject.name}");
            
            GetComponent<Renderer>().material.color = Color.green;
            hasBounced = true;

            Rigidbody rb = GetComponent<Rigidbody>();

            if (rb != null && !rb.isKinematic)
            {
              
                Vector3 direction = (transform.position - collision.transform.position).normalized;
                if (direction == Vector3.zero) direction = Vector3.right;

               
                transform.position += direction * 1.2f;
                // Задаем скорость разлета
                rb.linearVelocity = direction * 5f;
            }

            StartCoroutine(StopCube());
        }
    }

 
    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggers || !isGenerated || hasBounced) return;

        if (other.GetComponent<CubeManager>() != null)
        {
            Debug.Log($"[Триггер] {gameObject.name} пролетел сквозь {other.gameObject.name}");
            
            GetComponent<Renderer>().material.color = Color.green;
            hasBounced = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    // Общая корутина остановки
    IEnumerator StopCube()
    {
        yield return new WaitForSeconds(0.5f); 

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; 
        }
        Debug.Log(gameObject.name + " остановился");
    }
}