using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LuaTags : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public string Key;
        public string Value;
    }

    [SerializeField]
    private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    public void Set(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Key == key)
            {
                entries[i] = new Entry { Key = key, Value = value };
                return;
            }
        }
        entries.Add(new Entry { Key = key, Value = value });
    }

    public bool Remove(string key)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Key == key)
            {
                entries.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public string Get(string key)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Key == key) return entries[i].Value;
        return null;
    }
}
