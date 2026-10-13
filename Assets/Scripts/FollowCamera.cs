using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public GameObject playerObj;
    private Vector3 playerOffset = new Vector3(0, 12, -9);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = playerObj.transform.position + playerOffset;
    }
}
