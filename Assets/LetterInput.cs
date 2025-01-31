using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LetterInput : MonoBehaviour {
    public char Letter;
    public bool CapsLock;
    public bool Shift;

    public LetterInput(char letter, bool caps, bool shift)
    {
        Letter = letter;
        CapsLock = caps;
        Shift = shift;
    }
}
