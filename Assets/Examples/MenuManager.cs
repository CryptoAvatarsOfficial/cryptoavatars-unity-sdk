using CA;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;
using System.Collections.Generic;
using UnityStandardAssets.Characters.ThirdPerson;

public class MenuManager : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private RectTransform loginPanel;
    [SerializeField]
    private RectTransform avatarsPanel;
    private CryptoAvatars cryptoAvatars;

    [SerializeField]
    private Button useAsGuestBtn;
    [SerializeField]
    private Button nextPageBtn;
    [SerializeField]
    private Button prevPageBtn;


    /*[SerializeField]
    private WalletLogin metamaskLogin;*/

    public bool userLoggedIn { get; private set; }

    [SerializeField]
    private TMP_Text walletAddress;
    [SerializeField]
    private GameObject avatarCardPrefab;
    [SerializeField]
    private GameObject collectionCardPrefab;
    [SerializeField]
    private GameObject loadingSpinner;
    [SerializeField]
    private ScrollRect scrollViewAvatars;
    [SerializeField]
    private ScrollRect scrollViewCollections;
    [SerializeField]
    private TMP_InputField searchField;
    [SerializeField]
    private TMP_Text currentPageText;
    [SerializeField]
    private GameObject vrm;

    private bool loginPanelOn = true;

    private void Awake()
    {
        MainThreadDispatcher mainThreadDispatcher = MainThreadDispatcher.Instance;
        cryptoAvatars = new CryptoAvatars(mainThreadDispatcher);
    }
    private void Start()
    {
        vrm = GameObject.Find("AVATAR");

        useAsGuestBtn.onClick.AddListener(OnGuestEnter);

        nextPageBtn.onClick.AddListener(async () => await cryptoAvatars.NextPage(avatarsResult => LoadAndDisplayAvatars(avatarsResult)));
        prevPageBtn.onClick.AddListener(async () => await cryptoAvatars.PrevPage(avatarsResult => LoadAndDisplayAvatars(avatarsResult)));

        searchField.onValueChanged.AddListener(async (value) =>
        {
            CAModels.SearchAvatarsDto searchAvatar = new() { name = value };
            await Task.Run(() => cryptoAvatars.GetAvatars(LoadAndDisplayAvatars,searchAvatar.name));
        });
        LoadCollections();
    }
    public void OnGuestEnter()
    {
        Vector3 screenPos = loginPanelOn
            ? loginPanel.GetComponent<RectTransform>().position
            : avatarsPanel.GetComponent<RectTransform>().position;

        Vector3 hiddenPos = loginPanelOn
            ? avatarsPanel.GetComponent<RectTransform>().position
            : loginPanel.GetComponent<RectTransform>().position;

        avatarsPanel.GetComponent<RectTransform>().position = loginPanelOn ? screenPos : hiddenPos;
        loginPanel.GetComponent<RectTransform>().position = loginPanelOn ? hiddenPos : screenPos;
    }
    private void LoadAndDisplayAvatars(CAModels.NftsArray onAvatarsResult)
    {
        ClearScrollView();
        DisplayAvatars(onAvatarsResult);
    }
    private void ClearScrollView()
    {
        foreach (Transform child in scrollViewAvatars.content)
        {
            Destroy(child.gameObject);
        }
    }
    private async void DisplayAvatars(CAModels.NftsArray onAvatarsResult)
    {
        CAModels.Nft[] nfts = onAvatarsResult.nfts;
        foreach (var nft in nfts)
        {
            GameObject avatarCard = Instantiate(avatarCardPrefab, scrollViewAvatars.content.transform);
            CardAvatarController cardController = avatarCard.GetComponentInChildren<CardAvatarController>();

            cardController.SetAvatarData(
                nft.metadata.name,
                nft.metadata.asset,
                urlVRM => LoadVRMModel(urlVRM)
            );

            await cryptoAvatars.GetAvatarPreviewImage(nft.metadata.image, texture => cardController.LoadAvatarImage(texture));
        }
    }
    private async void LoadVRMModel(string urlVRM)
    {
        loadingSpinner.SetActive(true);
        await cryptoAvatars.GetAvatarVRMModel(urlVRM, (model, path) =>
        {
            ReplaceVRMModel(model);
            loadingSpinner.SetActive(false);
        });
    }
    private void ReplaceVRMModel(GameObject model)
    {
        Vector3 avatarPos = vrm.transform.position;
        Quaternion avatarRot = vrm.transform.rotation;
        Destroy(vrm);
        vrm = model;
        vrm.name = "AVATAR";
        vrm.transform.SetPositionAndRotation(avatarPos, avatarRot);
        vrm.GetComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Anims/Animator/ThirdPersonAnimatorController");
        vrm.AddComponent<ResizeCapsuleCollider>();
        vrm.AddComponent<ThirdPersonUserControl>();
        Camera.main.GetComponent<OrbitCamera>().targetPosition = vrm.transform;
    }
    public async void LoadMoreNfts()
    {
        await cryptoAvatars.NextPage(onAvatarsResult => LoadAndDisplayAvatars(onAvatarsResult));
    }
    public async void LoadPreviousNfts()
    {
        await cryptoAvatars.PrevPage(onAvatarsResult => LoadAndDisplayAvatars(onAvatarsResult));
    }
    public async void LoadCollections()
    {
        await cryptoAvatars.GetNFTCollections((collections) =>
        {          
            List<string> options = new List<string>();

            for (int i = 0; i < collections.nftCollections.Length; i++)
            {
                string name = collections.nftCollections[i].name;
                GameObject avatarCard = Instantiate(collectionCardPrefab, scrollViewCollections.content.transform);
                avatarCard.GetComponent<CardCollectionController>().SetCollectionData(
                    collections.nftCollections[i].name,
                    collections.nftCollections[i].logoImage,
                    async collectionName => await cryptoAvatars.GetAvatars(LoadAndDisplayAvatars, name)

                );
                cryptoAvatars.GetAvatarPreviewImage(collections.nftCollections[i].logoImage, 
                    texture => avatarCard.GetComponent<CardCollectionController>().LoadCollectionImage(texture)
                );
            }
        });
    }
}