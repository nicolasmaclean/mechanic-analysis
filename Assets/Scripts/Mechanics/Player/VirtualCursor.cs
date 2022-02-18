using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Abstracts VFX of cursor from Virtual Mouse.
/// </summary>
public class VirtualCursor : MonoBehaviour
{
    [SerializeField]
    Image _cursorCenter = null;

    void OnEnable()
    {
        // check fields for necessary values
    }

    void OnDisable()
    {
        
    }

    public void Activate()
    {
        Debug.Log("Activating Cursor");
    }

    public void Deactivate()
    {
        Debug.Log("Deactivating Cursor");
    }
}
