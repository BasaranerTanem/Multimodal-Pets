using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Consumer : MonoBehaviour
{
    public GameObject[] portions; // Changed to public
    public int currentIndex; // Changed to public
    private float lastChange;
    private float interval = 1f;
    public bool isConsuming = false; // Changed to public
    public int CurrentIndex
    {
        get { return currentIndex; }
        set { currentIndex = value; }
    }

    public GameObject[] Portions
    {
        get { return portions; }
    }

    public bool IsConsuming
    {
        get { return isConsuming; }
    }


    void Start()
    {
        bool skipFirst = transform.childCount > 4;
        portions = new GameObject[skipFirst ? transform.childCount-1 : transform.childCount];
        for (int i = 0; i < portions.Length; i++)
        {
            portions[i] = transform.GetChild(skipFirst ? i + 1 : i).gameObject;
            if (portions[i].activeInHierarchy)
                currentIndex = i;
        }
    }

    void Update()
    {
        if (isConsuming && Time.time - lastChange > interval)
        {
            Consume();
            lastChange = Time.time;
        }
    }

    void Consume()
    {
        if (currentIndex != portions.Length)
            portions[currentIndex].SetActive(false);
        currentIndex++;
        if (currentIndex > portions.Length)
            currentIndex = 0;
        else if (currentIndex == portions.Length)
            return;
        portions[currentIndex].SetActive(true);
    }

    public void StartConsuming()
    {
        isConsuming = true;
    }

    public void StopConsuming()
    {
        isConsuming = false;
    }
}
