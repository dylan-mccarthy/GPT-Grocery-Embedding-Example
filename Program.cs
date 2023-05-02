//Read in the data from the file FoodItems.json into a list of FoodItem objects
//The file FoodItems.json contains a list of food items, each food item has a name, a type, a description
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure;


public class Program
{
    public static void Main(){
        List<FoodItem> foodItems = new List<FoodItem>();
        using (StreamReader r = new StreamReader("FoodItems.json"))
        {
            string json = r.ReadToEnd();
            foodItems = JsonConvert.DeserializeObject<List<FoodItem>>(json);
        }

        string endpoint = "https://dm-openai-test-env.openai.azure.com/";

        OpenAIClient client = new OpenAIClient(new Uri(endpoint), new DefaultAzureCredential());

        //Check if file with embeddings already exists

        if(File.Exists(@"FoodItemsWithEmbeddings.json")){
            Console.WriteLine("Reading Embeddings from File");
            //Read in the embeddings from the file FoodItemsWithEmbeddings.json
            using (StreamReader r = new StreamReader("FoodItemsWithEmbeddings.json"))
            {
                string json = r.ReadToEnd();
                foodItems = JsonConvert.DeserializeObject<List<FoodItem>>(json);
            }
        }
        else{
            //Create embeddings for each food item in foodItems
            Console.WriteLine("Generating Embeddings for Food Items");
            foreach(FoodItem foodItem in foodItems){
                var embeddingOption = new EmbeddingsOptions(foodItem.description);
                var Response = client.GetEmbeddings("product-embedding", embeddingOption);
                foodItem.embeddings.AddRange(Response.Value.Data[0].Embedding);
            }

            //Write out food times and their embeddings to a file
            using (StreamWriter file = File.CreateText(@"FoodItemsWithEmbeddings.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, foodItems);
            }
        }

        var chatMessages = new List<ChatMessage>();
        chatMessages.Add(new ChatMessage(ChatRole.System, "You are a grocery store AI, you can provide answers to questions about food items. The items you have in stock are only those provided to you. If you do not have an item in stock, you can say that you do not have it. Only suggest recpies and meals that have items that you have in stock."));
        while(true){
            // Display prompt for the user to input question
            Console.WriteLine("Enter a question about food items:");
            string question = Console.ReadLine();

            Console.WriteLine("Generating Embeddings for Question");
            // Create embeddings for the question
            var embeddingOption = new EmbeddingsOptions(question);
            var Response = client.GetEmbeddings("product-embedding", embeddingOption);
            List<float> questionEmbeddings = new List<float>(Response.Value.Data[0].Embedding);
            
            Console.WriteLine("Calculating Cosine Similarity");
            //Create list of cosine similarities between the question and each food item
            List<float> similarities = new List<float>();
            foreach(FoodItem foodItem in foodItems){
                similarities.Add(foodItem.CosineSimilarity(questionEmbeddings));
            }

            //Find top 5 food items with highest cosine similarity
            List<FoodItem> top5 = new List<FoodItem>();
            for(int i = 0; i < 5; i++){
                int index = similarities.IndexOf(similarities.Max());
                top5.Add(foodItems[index]);
                similarities[index] = -1;
            }
            // Display the top 5 food items in yellow
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Top 5 Food Items:");
            foreach(FoodItem foodItem in top5){
                Console.WriteLine(foodItem.name + " " + foodItem.description);
            }
            Console.ForegroundColor = ConsoleColor.White;
            // Combine the top 5 food items and their description into a single string, removing duplicates
            string prompt = "";
            foreach(FoodItem foodItem in top5){
                prompt += foodItem.name + " " + foodItem.description + " ";
            }

            // Add the question to the prompt
            prompt += question;

            //Send to OpenAI API to generate answer
            chatMessages.Add(new ChatMessage(ChatRole.User, prompt));

            var chatOptions = new ChatCompletionsOptions();
            foreach(ChatMessage chatMessage in chatMessages){
                chatOptions.Messages.Add(chatMessage);
            }

            var response = client.GetChatCompletions("gpt3-5", chatOptions);

            Console.WriteLine("Answer:");
            //Display the answer
            Console.WriteLine(response.Value.Choices[0].Message.Content);

        }

    }
}