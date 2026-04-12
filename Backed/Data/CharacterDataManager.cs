using BackEnd;
using BACKND.Database;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CharacterDataManager : Singleton<CharacterDataManager>
{
    public static Client DBClient;
    private bool _initialized = false;

    private Dictionary<int, CharacterMasterData> masterById = new();
    private Dictionary<string, CharacterMasterData> masterByKey = new();
    private UserData userData = new();

    private const string USER_DATA_TABLE = "USER_CHARACTER";

    public bool IsOwned(int characterId)
    {
        return userData != null && userData.ownedCharacterIds.Contains(characterId);
    }

    public bool IsOwned(string characterKey)
    {
        if (masterByKey.TryGetValue(characterKey, out var data) == false)
            return false;

        return IsOwned(data.characterId);
    }

    public bool IsEquipped(int characterId)
    {
        return userData != null && userData.equippedCharacterId == characterId;
    }

    public bool IsEquipped(string characterKey)
    {
        if (masterByKey.TryGetValue(characterKey, out var data) == false)
            return false;

        return IsEquipped(data.characterId);
    }

    public CharacterMasterData GetMaster(int characterId)
    {
        masterById.TryGetValue(characterId, out var data);
        return data;
    }

    public CharacterMasterData GetMasterByKey(string characterKey)
    {
        masterByKey.TryGetValue(characterKey, out var data);
        return data;
    }

    public async void InitializeDatabase()
    {
        if (_initialized) return;

        DBClient = new Client("019c2c62-be25-730e-9ff9-ae397976a561");
        await DBClient.Initialize();

        _initialized = true;
        Debug.Log("데이터베이스 초기화 완료");

        await LoadMasterCharacter();
        await LoadOrCreateUserData();
    }

    #region Load

    public async Task LoadMasterCharacter()
    {
        var characters = await DBClient.From<CharacterMaster>().ToList();

        Debug.Log($"캐릭터 수: {characters.Count}");
        masterById.Clear();
        masterByKey.Clear();

        foreach (var c in characters)
        {
            CharacterMasterData data = new CharacterMasterData(c);

            masterById[data.characterId] = data;
            masterByKey[data.characterKey] = data;

            Debug.Log($"ID:{data.characterId} / KEY:{data.characterKey} / name:{data.name}");
        }

        Debug.Log($"마스터 캐릭터 로드 완료: {masterById.Count}");
    }

    public async Task LoadOrCreateUserData()
    {
        var bro = Backend.GameData.GetMyData(USER_DATA_TABLE, new Where());

        if (!bro.IsSuccess())
        {
            Debug.LogError($"유저 데이터 조회 실패 : {bro}");
            return;
        }

        LitJson.JsonData rows = bro.FlattenRows();

        if (rows.Count <= 0)
        {
            Debug.Log("유저 데이터가 없어서 기본값으로 생성합니다.");
            await CreateDefaultUserData();
            return;
        }

        LitJson.JsonData row = rows[0];
        userData = ParseUserData(row);

        Debug.Log($"유저 데이터 로드 완료 / 보유:{userData.ownedCharacterIds.Count} / 장착:{userData.equippedCharacterId}");
    }

    private async Task CreateDefaultUserData()
    {
        userData = new UserData();

        // 기본 지급 캐릭터 ID
        int defaultCharacterId = 1;

        userData.ownedCharacterIds.Add(defaultCharacterId);
        userData.equippedCharacterId = defaultCharacterId;

        Param param = new Param();
        param.Add("ownedCharacterIds", userData.ownedCharacterIds);
        param.Add("equippedCharacterId", userData.equippedCharacterId);

        var bro = Backend.GameData.Insert(USER_DATA_TABLE, param);

        if (bro.IsSuccess())
            Debug.Log("기본 유저 데이터 생성 완료");
        else
            Debug.LogError($"기본 유저 데이터 생성 실패 : {bro}");

        await UniTask.CompletedTask;
    }

    private UserData ParseUserData(LitJson.JsonData row)
    {
        UserData data = new UserData();

        if (row.ContainsKey("equippedCharacterId"))
            data.equippedCharacterId = int.Parse(row["equippedCharacterId"].ToString());

        if (row.ContainsKey("ownedCharacterIds"))
        {
            LitJson.JsonData ownedList = row["ownedCharacterIds"];

            for (int i = 0; i < ownedList.Count; i++)
                data.ownedCharacterIds.Add(int.Parse(ownedList[i].ToString()));
        }

        return data;
    }

    #endregion

    #region Save

    public void SaveUserData()
    {
        Where where = new Where();

        Param param = new Param();
        param.Add("ownedCharacterIds", userData.ownedCharacterIds);
        param.Add("equippedCharacterId", userData.equippedCharacterId);

        var bro = Backend.GameData.Update(USER_DATA_TABLE, where, param);

        if (bro.IsSuccess())
            Debug.Log("유저 데이터 저장 완료");
        else
            Debug.LogError($"유저 데이터 저장 실패 : {bro}");
    }

    #endregion

    #region Character Logic

    public bool AddCharacterById(int characterId)
    {
        if (masterById.ContainsKey(characterId) == false)
        {
            Debug.LogWarning($"존재하지 않는 캐릭터 ID : {characterId}");
            return false;
        }

        if (IsOwned(characterId))
        {
            Debug.Log($"이미 보유중인 캐릭터 : {characterId}");
            return false;
        }

        userData.ownedCharacterIds.Add(characterId);
        SaveUserData();

        Debug.Log($"캐릭터 획득 완료 : {characterId}");
        return true;
    }

    public bool AddCharacterByKey(string characterKey)
    {
        if (masterByKey.TryGetValue(characterKey, out var data) == false)
        {
            Debug.LogWarning($"존재하지 않는 캐릭터 Key : {characterKey}");
            return false;
        }

        return AddCharacterById(data.characterId);
    }

    public bool EquipCharacterById(int characterId)
    {
        if (masterById.ContainsKey(characterId) == false)
        {
            Debug.LogWarning($"존재하지 않는 캐릭터 ID : {characterId}");
            return false;
        }

        if (IsOwned(characterId) == false)
        {
            Debug.LogWarning($"보유하지 않은 캐릭터는 장착할 수 없음 : {characterId}");
            return false;
        }

        if (userData.equippedCharacterId == characterId)
        {
            Debug.Log($"이미 장착 중인 캐릭터 : {characterId}");
            return true;
        }

        userData.equippedCharacterId = characterId;
        SaveUserData();

        Debug.Log($"캐릭터 장착 완료 : {characterId}");
        return true;
    }

    public bool EquipCharacterByKey(string characterKey)
    {
        if (masterByKey.TryGetValue(characterKey, out var data) == false)
        {
            Debug.LogWarning($"존재하지 않는 캐릭터 Key : {characterKey}");
            return false;
        }

        return EquipCharacterById(data.characterId);
    }

    #endregion

    public IEnumerable<CharacterMasterData> GetAllCharacters()
    {
        return masterById.Values;
    }

    public int GetEquippedCharacterId()
    {
        return userData != null ? userData.equippedCharacterId : -1;
    }

    public string GetEquippedCharacterKey()
    {
        var master = GetMaster(GetEquippedCharacterId());
        return master != null ? master.characterKey : string.Empty;
    }
}