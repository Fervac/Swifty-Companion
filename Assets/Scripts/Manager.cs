using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    #region SECRET
    private readonly static string UID = "bffeabc889351bd7e4512ec1c4124cd5d26a82a2296c74247d341bcce2a49efa";
    private readonly static string SECRET = "7ffbddaa2949e9804214ec62a47123450465f0be8c903780e8b12c53d43322f9";
    #endregion

    #region SINGLETON PATTERN
    public static Manager _instance;
    public static Manager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<Manager>();

                if (_instance == null)
                {
                    GameObject container = new GameObject("Manager");
                    _instance = container.AddComponent<Manager>();
                }

                DontDestroyOnLoad(_instance);
            }

            return _instance;
        }
    }
    #endregion

    public InputField mainInputField;
    public string login;
    public Image img;

    private void Awake()
    {
        Screen.SetResolution(720, 1280, false);
    }

    [Serializable]
    public class Token
    {
        public string access_token;
        public string token_type;
        public string expires_in;
        public string scope;
        public string created_at;
    }

    public void GetToken()
    {
        login = mainInputField.text;
        StartCoroutine(PostRequest());
    }

    private IEnumerator PostRequest()
    {
        Dictionary<string, string> content = new Dictionary<string, string>();
        //Fill key and value
        content.Add("grant_type", "client_credentials");
        content.Add("client_id", UID);
        content.Add("client_secret", SECRET);

        UnityWebRequest www = UnityWebRequest.Post("https://api.intra.42.fr/oauth/token", content);
        yield return www.SendWebRequest();

        Token deserializedPostData = JsonUtility.FromJson<Token>(www.downloadHandler.text);

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError(www.error);
            yield break;
        }

        StartCoroutine(makeRequestWithToken(deserializedPostData));
    }

    IEnumerator makeRequestWithToken(Token token)
    {
        var url = "https://api.intra.42.fr/v2/users/" + login;

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Authorization", "Bearer " + token.access_token);
        yield return www.SendWebRequest();

        var user = JsonUtility.FromJson<UserObject>(www.downloadHandler.text);

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError(www.error);
            yield break;
        }

        StartCoroutine(LoadFromWeb(user.image_url));
    }


    IEnumerator LoadFromWeb(string url)
    {
        UnityWebRequest wr = new UnityWebRequest(url);
        DownloadHandlerTexture texDl = new DownloadHandlerTexture(true);
        wr.downloadHandler = texDl;
        yield return wr.SendWebRequest();
        if (wr.result == UnityWebRequest.Result.Success)
        {
            Texture2D t = texDl.texture;
            Sprite s = Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                Vector2.zero, 1f);
            img.sprite = s;
        }
    }

    [Serializable]
    public class UserObject
    {
        public string id;
        public string email;
        public string login;
        public string first_name;
        public string last_name;
        public string usual_first_name;
        public string url;
        public string phone;
        public string displayname;
        public string usual_full_name;
        public string image_url;
        public string staff;
        public string correction_point;
        public string wallet;
        public string grade;
        public string created_at;
        public string updated_at;
    }
    

   [Serializable]
    public class RootObject
    {
        public string users;
    }
}
