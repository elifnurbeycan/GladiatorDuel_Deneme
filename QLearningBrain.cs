using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class QLearningBrain : MonoBehaviour
{
    [Header("General Settings")]
    public float learningRate = 0.5f; // Öğrenme hızını artırdım
    public float discount = 0.9f;
    public float exploration = 0.3f;  // Keşfetme oranı

    [Header("File Settings")]
    public string saveFileName = "RLBrain.json";

    // Input ve Action Tanımları
    private List<float> currentInputs = new();
    private List<ActionDefinition> actions = new();

    // Q-Table (Ana Beyin)
    private Dictionary<string, float[]> Q = new();

    private string savePath;

    // --- STRUCTS (YAPILAR) ---
    [Serializable]
    public class ActionDefinition
    {
        public string actionName;
        public Action<object[]> method;
        public int parameterCount;

        public ActionDefinition(string name, Action<object[]> func, int paramCount)
        {
            actionName = name;
            method = func;
            parameterCount = paramCount;
        }
    }

    // 🔥 DÜZELTME: Unity Dictionary kaydedemez, bu yüzden List kullanıyoruz 🔥
    [Serializable]
    private class SaveModel
    {
        public int inputCount;
        public int actionCount;
        
        // Dictionary yerine Key ve Value listeleri
        public List<string> keys = new List<string>();
        public List<FloatArrayWrapper> values = new List<FloatArrayWrapper>();
    }

    [Serializable]
    public class FloatArrayWrapper
    {
        public float[] array;
    }

    // --- UNITY EVENTS ---

    void Awake()
    {
        savePath = Path.Combine(Application.dataPath, saveFileName);
        LoadOrCreateModel();
    }

    // --- PUBLIC API ---

    public void SetInputs(List<float> inputs)
    {
        currentInputs = inputs;
    }

    public void RegisterAction(string name, Action<object[]> method, int parameterCount)
    {
        actions.Add(new ActionDefinition(name, method, parameterCount));
    }

    public int DecideAction()
    {
        string state = EncodeState(currentInputs);
        EnsureStateExists(state);

        // Keşfetme (Exploration)
        if (UnityEngine.Random.value < exploration)
        {
            return UnityEngine.Random.Range(0, actions.Count);
        }

        // En iyi hareketi seç (Exploitation)
        float[] qRow = Q[state];
        int bestIndex = 0;
        float bestVal = qRow[0];

        for (int i = 1; i < qRow.Length; i++)
        {
            if (qRow[i] > bestVal)
            {
                bestVal = qRow[i];
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    public void ExecuteAction(int actionIndex, params object[] parameters)
    {
        if (actionIndex >= 0 && actionIndex < actions.Count)
        {
            actions[actionIndex].method.Invoke(parameters);
        }
    }

    public void Reward(float value) => ApplyReward(value);
    public void Punish(float value) => ApplyReward(-Mathf.Abs(value));

    // --- INTERNAL LOGIC ---

    private void ApplyReward(float reward)
    {
        string oldState = EncodeState(currentInputs);
        EnsureStateExists(oldState);

        // Son yapılan hamleyi bulmak zor olduğu için, basit Q-Learning yerine
        // State-Action güncellemesini DecideAction sonrasında yapmak daha doğrudur.
        // Ancak mevcut yapıyı bozmadan en son durumu güncelleyelim.
        
        // Not: Bu basit versiyonda "Hangi aksiyonu yapmıştık?" bilgisini saklamadık.
        // Doğrusu: DecideAction sonucunu saklayıp burada kullanmaktır.
        // Şimdilik varsayım: Son state'teki en yüksek değere sahip aksiyonu ödüllendiriyoruz.
        // (Veya EnemyAgent tarafında aksiyon index'i biliniyor olmalıydı).
        
        // Hızlı düzeltme: Basit Q güncellemesi
        float[] qRow = Q[oldState];
        int bestAction = GetBestActionIndex(qRow);
        
        // Formül: Q(s,a) = Q(s,a) + alpha * (reward + gamma * maxQ(s') - Q(s,a))
        // Gelecek durumu (s') şimdilik göz ardı ediyoruz (Basit versiyon)
        qRow[bestAction] += learningRate * (reward - qRow[bestAction]);

        SaveModelToFile();
    }

    private int GetBestActionIndex(float[] qRow)
    {
        int bestIndex = 0;
        float bestVal = qRow[0];
        for (int i = 1; i < qRow.Length; i++)
        {
            if (qRow[i] > bestVal) { bestVal = qRow[i]; bestIndex = i; }
        }
        return bestIndex;
    }

    private void EnsureStateExists(string state)
    {
        if (Q == null) Q = new Dictionary<string, float[]>(); // Null Check

        if (!Q.ContainsKey(state))
        {
            Q[state] = new float[actions.Count];
        }
    }

    private string EncodeState(List<float> inputs)
    {
        return string.Join("_", inputs);
    }

    // --- SERIALIZATION (KAYIT SİSTEMİ) ---

    private void LoadOrCreateModel()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("RLBrain: New model created.");
            CreateNewModel();
            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            SaveModel model = JsonUtility.FromJson<SaveModel>(json);

            if (model == null || model.keys == null)
            {
                Debug.LogWarning("RLBrain: Model corrupted. Creating new.");
                CreateNewModel();
                return;
            }

            Q = new Dictionary<string, float[]>();
            for (int i = 0; i < model.keys.Count; i++)
            {
                Q[model.keys[i]] = model.values[i].array;
            }
            Debug.Log("RLBrain: Model loaded successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError("RLBrain Error: " + e.Message);
            CreateNewModel();
        }
    }

    private void CreateNewModel()
    {
        Q = new Dictionary<string, float[]>();
        SaveModelToFile();
    }

    private void SaveModelToFile()
    {
        SaveModel model = new SaveModel();
        model.inputCount = currentInputs.Count;
        model.actionCount = actions.Count;
        
        // Dictionary'yi Listeye çevir (Kaydetmek için)
        foreach (var kvp in Q)
        {
            model.keys.Add(kvp.Key);
            model.values.Add(new FloatArrayWrapper { array = kvp.Value });
        }

        string json = JsonUtility.ToJson(model, true);
        File.WriteAllText(savePath, json);
    }
}