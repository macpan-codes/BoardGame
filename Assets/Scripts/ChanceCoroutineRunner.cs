using System.Collections;
using UnityEngine;

public class ChanceCoroutineRunner : MonoBehaviour
{
    public Coroutine Run(IEnumerator routine)
    {
        if (routine == null)
            return null;

        return StartCoroutine(routine);
    }
}