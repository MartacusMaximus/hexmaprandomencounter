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
public sealed class MythicTableColumn
{
    public string header;
    [TextArea] public List<string> values = new List<string>();
}

[Serializable]
public sealed class MythicRollTable
{
    public string title;
    public List<MythicTableColumn> columns = new List<MythicTableColumn>();
}
