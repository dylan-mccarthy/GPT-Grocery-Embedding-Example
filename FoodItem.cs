//class to hold food item embaddings, each food item has a name, a type, a description and a list of embeddings
//the embeddings are a list of floats
//the class also has a method to calculate the cosine similarity between two food items

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class FoodItem{
    public string name;
    public string type;
    public string description;
    public List<float> embeddings;
    public int TokenCount { get; set; }

    public FoodItem(string name, string type, string description){
        this.name = name;
        this.type = type;
        this.description = description;
        this.embeddings = new List<float>();
        this.TokenCount = 0;
    }

    public float CosineSimilarity(List<float> other){
        float dotProduct = 0;
        float normA = 0;
        float normB = 0;
        for(int i = 0; i < this.embeddings.Count; i++){
            dotProduct += this.embeddings[i] * other[i];
            normA += this.embeddings[i] * this.embeddings[i];
            normB += other[i] * other[i];
        }
        return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }

}