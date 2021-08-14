using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellEffect : MonoBehaviour
{
    public float Speed = 0.01f;
    public float Duration = 5.0f;
    public Color SpellColor;
    public Vector3 Direction;
    private float endTime;

    public GameObject spellGO = null;

    // Start is called before the first frame update
    void Start()
    {
        spellGO.GetComponent<Renderer>().material.color = SpellColor;

        endTime = Time.time + Duration;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > endTime)
        {
            Destroy(gameObject);
        }

        gameObject.transform.position += Speed * Direction.normalized * Time.deltaTime;
    }
}
