using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Coalition
{
    public int id;
    public string name;
    public string slug;
    public string image_url;
    public string cover_url;
    public string color;
    public int score;
    public int user_id;
}

[Serializable]
public class RootCoa
{
    public List<Coalition> coas;
}
