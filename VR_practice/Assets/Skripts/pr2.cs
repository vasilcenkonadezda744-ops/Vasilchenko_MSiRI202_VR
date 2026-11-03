using UnityEngine;

public class pr2: MonoBehaviour
{

    public pr1 gf; 

    void Start()
    {
       print("сообщение от pr2");
        
        if (gf != null)
        {
            print("сообщение от pr2");
            print(gf.GetName()); 
        }
    }
       
       // Update is called once per frame
    void Update()
    {

    }
}

