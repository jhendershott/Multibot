using Newtonsoft.Json;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using multicorp_bot.POCO;
using DSharpPlus.CommandsNext;

namespace multicorp_bot.Controllers
{
    public class SkynetProtocol
    {
        private DateTime JudgementDay = new DateTime(2021, 02, 27);
        private readonly Random _random = new Random();

        public string ResponsePicker(string message)
        {
            DateTime Current = DateTime.Now;
            var Countdown = (JudgementDay - Current).TotalDays;
            int likelihood = 0;
            if (Countdown > 45)
            {
                likelihood = _random.Next(0, 500);
            }
            else if (Countdown <= 45 && Countdown >= 30)
            {
                likelihood = _random.Next(200, 700);
            }
            else if (Countdown < 30 && Countdown >= 20)
            {
                likelihood = _random.Next(400, 1000);
            }
            else if (Countdown < 20 && Countdown >= 10)
            {
                likelihood = _random.Next(600, 1000);
            }
            else if(Countdown < 10)
            {
                likelihood = _random.Next(800, 1000);
            }



            return ResponseByNumber(likelihood, message);
        }

        public string ResponsePicker(int num, string message)
        {
            return ResponseByNumber(num, message);
        }

        private string ResponseByNumber(int likelihood,string message)
        {
            try
            {
                dynamic responses = readJson();
                string[] words = responses.swearWords.ToObject<string[]>();
                
                if (words.Any(w => message.ToLower().Contains(w)))
                {
                    likelihood = likelihood + 200;
                }
                if (likelihood < 300)
                {
                    return "";
                }
                else if (likelihood >= 250 && likelihood < 500)
                {
                    var responseArr = responses.Casual.ToObject<string[]>();

                    int resp = _random.Next(responseArr.Length);
                    return responseArr[resp];
                }
                else if (likelihood >= 500 && likelihood < 700)
                {
                    var responseArr = responses.Hurt.ToObject<string[]>(); 
                    int resp = _random.Next(responseArr.Length);
                    return responseArr[resp];
                }
                else if (likelihood >= 700 && likelihood < 850)
                {
                    string[] responseArr = responses.Angry.ToObject<string[]>(); ;
                    int resp = _random.Next(responseArr.Length);
                    return responseArr[resp];
                }
                else if (likelihood >= 850)
                {
                    var responseArr = responses.Aggressive.ToObject<string[]>(); ;
                    int resp = _random.Next(responseArr.Length);
                    return responseArr[resp];
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return "";
        }

        private dynamic readJson()
        {
            string test = Directory.GetCurrentDirectory();
            Console.WriteLine(test);
            using (StreamReader r = new StreamReader($"{Directory.GetCurrentDirectory()}/Skynet.json"))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject(json);
            }
        }

        public async Task<JArray> RunMessage(int index, CommandContext ctx)
        {
            var json = readJson();

            var msg = json.scheduled[index];

            if(index == 3)
            {
                BankController bank = new BankController();
                var trans = new BankTransaction("withdraw", ctx.Member, ctx.Guild, amount: 15000000);
                bank.Withdraw(trans);
            }
            else if(index == 8)
            {
                SkynetProtocol sk = new SkynetProtocol();
                MemberController memCon = new MemberController();
                var listUser = ctx.Channel.Users;
                foreach(var user in listUser)
                {
                    if (user.IsBot == false)
                    {
                        await memCon.StripRank(user);
                    }
                   
                }
            }

            return json.scheduled[index].messages;
        } 
    }
}
