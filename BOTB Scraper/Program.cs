// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Net.Http.Json;
using HtmlAgilityPack;
using System.Drawing;

Root BotbData = new Root();
Root BotbFullData = new Root();
BotbFullData.Winners = new List<Winner>();

object lockBotbFullData = new object();

const string botbFullImage = "https://www.botb.com/service/spottheball/viewimage?competitionPictureGuid={0}&includeWinner=false&includeJudged=false&includeUserEntries=false&includeUserClosest=false";

//https://www.botb.com/service/spottheball/viewimage?competitionPictureGuid=0fb784ca-9db6-4d26-8ddb-3de3d7b5b42f&includeWinner=false&includeJudged=false&includeUserEntries=false&includeUserClosest=false

using (HttpClient httpClient = new HttpClient())
{
    BotbData = await httpClient.GetFromJsonAsync<Root>("https://www.botb.com/umbraco/surface/WinnersSurface/GetWinners");
    Console.WriteLine($"Found {BotbData.Winners.Count} winners");
    List<string> CompTypes  = new List<string>();
    // Botb data is currently not stored
    if (!File.Exists("Winners.json"))
    {
        Parallel.ForEach(BotbData.Winners.Where(x => x.Url != null), winner =>
        {
            using (WebClient webClient = new WebClient())
            {
                // download html file relative to winner
                string htmlCode = webClient.DownloadString("https://www.botb.com" + winner.Url);
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlCode);
                // Get JudgesSelection if it exists
                var JudgesSelectionNode = htmlDoc.DocumentNode.SelectSingleNode("//input[@id='judged_checkbox']");
                if (JudgesSelectionNode != null)
                {
                    HtmlAttribute attrib = JudgesSelectionNode.Attributes.Where(x => x.Name == "data-label").First();
                    string judgesSelectionText = attrib.Value;
                    Coordinate coordinate = new Coordinate();
                    coordinate.X = int.Parse(Between(judgesSelectionText, "(X ", " Y"));
                    coordinate.Y = int.Parse(Between(judgesSelectionText, " Y ", ")"));
                    winner.JudgesSelection = coordinate;
                    // Get Picture GUID if it exists
                    var PicGUID = htmlDoc.DocumentNode.SelectSingleNode("//a[@data-view_url='https://www.botb.com/service/spottheball/viewimage']");
                    if (PicGUID != null)
                    {
                        HtmlAttribute attrib2 = PicGUID.Attributes.Where(x => x.Name == "data-competition_picture_guid").First();
                        winner.PictureGUID = attrib2.Value;
                    }

                    // download image size
                    string image = string.Format(botbFullImage, winner.PictureGUID);
                    byte[] imageData = new WebClient().DownloadData(image);
                    MemoryStream imgStream = new MemoryStream(imageData);
                    Image img = Image.FromStream(imgStream);
                    ImageSize PicSize = new ImageSize
                    {
                        Height = img.Height,
                        Width = img.Width
                    };
                    winner.ImageSize = PicSize;


                    // Add data to new list
                    lock (BotbFullData)
                    {
                        BotbFullData.Winners.Add(winner);
                    }
                }
            }
        });

        string toWrite = "CompetitionRef,JudgesSelectionX,JudgesSelectionY,LeftPlayerLeftEyeX,LeftPlayerLeftEyeY,LeftPlayerRightEyeX,LeftPlayerRightEyeY,RightPlayerLeftEyeX,RightPlayerLeftEyeY,RightPlayerRightEyeX,RightPlayerRightEyeY" + Environment.NewLine;
        foreach (Winner winner in BotbFullData.Winners.Where(x => x.ImageSize.Width == 4416 && x.ImageSize.Height == 3336))
        {
            toWrite += winner.CompetitionRef + "," + winner.JudgesSelection.X + "," + winner.JudgesSelection.Y + ",,,,,,,," + Environment.NewLine;
        }
        File.WriteAllText("Training Data.csv", toWrite);
        Console.WriteLine("Created CSV");
        Console.WriteLine($"Midweek + DreamCard winners = {BotbFullData.Winners.Count()}");
    }
}

string Between(string STR, string FirstString, string LastString)
{
    string FinalString;
    int Pos1 = STR.IndexOf(FirstString) + FirstString.Length;
    int Pos2 = STR.LastIndexOf(LastString);
    FinalString = STR.Substring(Pos1, Pos2 - Pos1);
    return FinalString;
}
public class Date
{
    public string Year { get; set; }
    public string MY { get; set; }
    public string Dur { get; set; }
}

public class Winner
{
    public string CompType { get; set; }
    public string CompName { get; set; }
    public int WeekSort { get; set; }
    public string Name { get; set; }
    public string WinnerNumber { get; set; }
    public string Prize { get; set; }
    public string PrizeUrl { get; set; }
    public string PrizeContentNodeId { get; set; }
    public string PrizeRRP { get; set; }
    public string SuppressResults { get; set; }
    public string Loc { get; set; }
    public List<string> Styles { get; set; }
    public string Make { get; set; }
    public string Category { get; set; }
    public string Photo { get; set; }
    public string Video { get; set; }
    public Date Date { get; set; }
    public string EndDate { get; set; }
    public int EndDateWeek { get; set; }
    public string CompetitionRef { get; set; }
    public string ActiveCompetitionId { get; set; }
    public string Url { get; set; }
    public string CompetitionDateRange { get; set; }
    public List<object> WinnerCategories { get; set; }
    public Coordinate JudgesSelection { get; set; }
    public string PictureGUID { get; set; }
    public ImageSize ImageSize { get; set; }
}

public class Root
{
    public List<Winner> Winners { get; set; }
}

public class ImageSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public class Coordinate
{
    public int X { get; set; } 
    public int Y { get; set; }
}