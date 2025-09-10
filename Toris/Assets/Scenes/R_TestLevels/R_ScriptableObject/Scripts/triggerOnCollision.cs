using UnityEngine;

public class triggerOnCollision : MonoBehaviour
{

    public GameObject rock;
    public GameObject promtText;
    bool isRockInstantiated = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Y SOAB I'm In");
        if (!isRockInstantiated)
        {
            Instantiate(rock, new(12, 2, 10), Quaternion.identity);
            isRockInstantiated = true;
        }
        promtText.SetActive(true);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        promtText.SetActive(false);
    }
}
