using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MythCastEntry
{
    public string name;
    [TextArea] public string statBlock;
    [TextArea] public string notes;
}

[Serializable]
public sealed class MythFlavorTable
{
    public string title;
    [TextArea] public List<string> rows = new List<string>();
}
