using System;
using System.Collections;
using System.Collections.Generic;
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


    public Token token;

    public GameObject searchPanel;
    public GameObject displayPanel;
    public InputField mainInputField;
    public string login;
    public Image img;
    public Text UserErrorText;
    public Text DisplayNameText;

    public GameObject SkillContent;
    public GameObject SkillPrefab;

    public GameObject ProjectContent;
    public GameObject ProjectPrefab;
    public Sprite check;
    public Sprite nocheck;


    public Image coaBackground;

    public Sprite federation;
    public Sprite assembly;
    public Sprite order;
    public Sprite alliance;

    // Progress bar
    public int maximum;
    public float current;
    public Image mask;
    public Text levelText;

    // Infos
    public Text walletText;
    public Text evaluationText;

    private void Awake()
    {
        Screen.SetResolution(720, 1280, false);
    }

    private void Start()
    {
        StartCoroutine(SetToken());
    }

    public void SwitchShowWindow(GameObject window)
    {
        window.SetActive(!window.activeInHierarchy);
    }

    public void SwitchView()
    {
        SwitchShowWindow(searchPanel);
        SwitchShowWindow(displayPanel);
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

        StartCoroutine(makeRequestWithToken(token));
    }

    private IEnumerator SetToken()
    {
        Dictionary<string, string> content = new Dictionary<string, string>();
        //Fill key and value
        content.Add("grant_type", "client_credentials");
        content.Add("client_id", UID);
        content.Add("client_secret", SECRET);

        UnityWebRequest www = UnityWebRequest.Post("https://api.intra.42.fr/oauth/token", content);
        yield return www.SendWebRequest();

        token = JsonUtility.FromJson<Token>(www.downloadHandler.text);

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(www.error);
            yield break;
        }

        yield return new WaitForSeconds(float.Parse(token.expires_in));

        SetToken();
    }

    IEnumerator makeRequestWithToken(Token token)
    {
        var url = "https://api.intra.42.fr/v2/users/" + login;

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Authorization", "Bearer " + token.access_token);
        yield return www.SendWebRequest();

        if (www.downloadHandler.text == "{}")
        {
            StartCoroutine(UserErrorCoroutine());

            Debug.Log(www.error);
            yield break;
        }

        var user = JsonUtility.FromJson<RootObject>(www.downloadHandler.text);

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(www.error);
            yield break;
        }
        else
        {
            DisplayNameText.text = user.displayname;
            walletText.text = "wallet: " + user.wallet.ToString() + "₳";
            evaluationText.text = "evaluation points: " + user.correction_point.ToString();

            SetSkills(user);

            SetProjects(user);

            StartCoroutine(LoadFromWeb(user.image_url));
            
            StartCoroutine(LoadCoa(user, token));
        }
    }

    private IEnumerator LoadCoa(RootObject user, Token token)
    {
        var url = "https://api.intra.42.fr/v2/users/" + user.id + "/coalitions";

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Authorization", "Bearer " + token.access_token);
        yield return www.SendWebRequest();

        if (www.downloadHandler.text == "{}")
        {
            StartCoroutine(UserErrorCoroutine());

            Debug.Log(www.error);
            yield break;
        }

        if (www.downloadHandler.text.Contains("The Federation"))
        {
            coaBackground.sprite = federation;
        }
        else if (www.downloadHandler.text.Contains("The Order"))
        {
            coaBackground.sprite = order;
        }
        else if (www.downloadHandler.text.Contains("The Alliance"))
        {
            coaBackground.sprite = alliance;
        }
        else if (www.downloadHandler.text.Contains("The Assembly"))
        {
            coaBackground.sprite = assembly;
        }

        SwitchView();
    }

    private void SetProjects(RootObject user)
    {
        foreach (Transform child in ProjectContent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (ProjectsUsers projectsUsers in user.projects_users)
        {
            GameObject tmp = Instantiate(ProjectPrefab);
            tmp.transform.SetParent(ProjectContent.transform);

            tmp.GetComponent<ProjectScr>().projectName.text = projectsUsers.project.name;
            tmp.GetComponent<ProjectScr>().projectGrade.text = projectsUsers.final_mark.ToString();
            if (projectsUsers.final_mark >= 75)
            {
                tmp.GetComponent<ProjectScr>().validateSpr.sprite = check;
            }
            else
            {
                tmp.GetComponent<ProjectScr>().validateSpr.sprite = nocheck;
            }
        }
    }

    private void SetSkills(RootObject user)
    {
        foreach (Transform child in SkillContent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        foreach (CursusUsers cursusUsers in user.cursus_users)
        {
            if (cursusUsers.cursus_id == 21)
            {
                SetLevel(cursusUsers);

                foreach (Skill skill in cursusUsers.skills)
                {
                    GameObject tmp = Instantiate(SkillPrefab);
                    tmp.transform.SetParent(SkillContent.transform);

                    tmp.GetComponent<SkillScr>().skillName.text = skill.name;
                    tmp.GetComponent<SkillScr>().skillLevel.text = skill.level.ToString();

                    float percentage = (float)skill.level / 21;
                    tmp.GetComponent<SkillScr>().mask.fillAmount = percentage;
                }
            }
        }
    }

    private void SetLevel(CursusUsers user)
    {
        current = (float)user.level;
        float fillAmount = current / (float)maximum;
        mask.fillAmount = fillAmount;
        levelText.text = "level: " + user.level.ToString();
    }

    IEnumerator UserErrorCoroutine()
    {
        UserErrorText.text = "Incorrect Login";

        yield return new WaitForSeconds(2);

        UserErrorText.text = "";
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
    public class RootObject
    {
        public int id;
        public string email;
        public string login;
        public string first_name;
        public string last_name;
        public object usual_first_name;
        public string url;
        public string phone;
        public string displayname;
        public string usual_full_name;
        public string image_url;
        public bool staff;
        public int correction_point;
        public string pool_month;
        public string pool_year;
        public object location;
        public int wallet;
        public DateTime anonymize_date;
        public DateTime created_at;
        public DateTime updated_at;
        public List<object> groups;
        public List<CursusUsers> cursus_users;
        public List<ProjectsUsers> projects_users;
        public List<LanguagesUsers> languages_users;
        public List<Achievement> achievements;
        public List<object> titles;
        public List<object> titles_users;
        public List<object> partnerships;
        public List<Patroned> patroned;
        public List<object> patroning;
        public List<object> expertises_users;
        public List<object> roles;
        public List<Campu> campus;
        public List<CampusUsers> campus_users;
    }

    [Serializable]
    public class CursusUsers
    {
        public string grade;
        public double level;
        public List<Skill> skills;
        public DateTime blackholed_at;
        public int id;
        public DateTime begin_at;
        public object end_at;
        public int cursus_id;
        public bool has_coalition;
        public DateTime created_at;
        public DateTime updated_at;
        public User user;
        public Cursus cursus;
    }

    [Serializable]
    public class Skill
    {
        public int id;
        public string name;
        public double level;
    }

    [Serializable]
    public class User
    {
        public int id;
        public string login;
        public string url;
        public DateTime created_at;
        public DateTime updated_at;
    }

    [Serializable]
    public class Cursus
    {
        public int id;
        public DateTime created_at;
        public string name;
        public string slug;
    }

    [Serializable]
    public class ProjectsUsers
    {
        public int id;
        public int occurrence;
        public int final_mark;
        public string status;
        public bool validated;
        public int current_team_id;
        public Project project;
        public List<int> cursus_ids;
        public DateTime marked_at;
        public bool marked;
        public DateTime retriable_at;
        public DateTime created_at;
        public DateTime updated_at;
    }

    [Serializable]
    public class Project
    {
        public int id;
        public string name;
        public string slug;
        public int parent_id;
    }

    [Serializable]
    public class LanguagesUsers
    {
        public int id;
        public int language_id;
        public int user_id;
        public int position;
        public DateTime created_at;
    }

    [Serializable]
    public class Achievement
    {
        public int id;
        public string name;
        public string description;
        public string tier;
        public string kind;
        public bool visible;
        public string image;
        public object nbr_of_success;
        public string users_url;
    }

    [Serializable]
    public class Patroned
    {
        public int id;
        public int user_id;
        public int godfather_id;
        public bool ongoing;
        public DateTime created_at;
        public DateTime updated_at;
    }

    [Serializable]
    public class Campu
    {
        public int id;
        public string name;
        public string time_zone;
        public Language language;
        public int users_count;
        public int vogsphere_id;
        public string country;
        public string address;
        public string zip;
        public string city;
        public string website;
        public string facebook;
        public string twitter;
        public bool active;
        public string email_extension;
        public bool default_hidden_phone;
    }

    [Serializable]
    public class Language
    {
        public int id;
        public string name;
        public string identifier;
        public DateTime created_at;
        public DateTime updated_at;
    }

    [Serializable]
    public class CampusUsers
    {
        public int id;
        public int user_id;
        public int campus_id;
        public bool is_primary;
        public DateTime created_at;
        public DateTime updated_at;
    }
}
