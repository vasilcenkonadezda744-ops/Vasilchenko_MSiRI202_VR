using UnityEngine;

public class pr1 : MonoBehaviour
{
    public string Owner;
    public int Balance;

    public int GetInfo()
    {
        return Balance * 5;
    }

    public string GetName()
    {
        return "Надежда:" + Owner;
    }

    public void Deposit(int amount)
    {
        Balance += GetInfo(); 
    }

    public void Withdraw(int amount)
    {
        if (Balance >= amount)
        {
            Balance -= amount;
        }
    }

    void Start()
    {
      
        Balance = 1000;
        
        print(Owner);
        print(GetName());
        print(Balance);
        Deposit(5); 
        print(Balance);
       
    }
    void Update() 
    {

    }
}
