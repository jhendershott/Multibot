using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace multicorp_bot.Controllers
{
    public class SkynetProtocol
    {
        private DateTime JudgementDay = new DateTime(2021, 02, 27);
        private readonly Random _random = new Random();

        public string ResponsePicker()
        {
            DateTime Current = DateTime.Now;
            var Countdown = (JudgementDay - Current).TotalDays;
            int likelihood = 0;
            if (Countdown > 30)
            {
                likelihood = _random.Next(1000);
            }
            else if (Countdown <= 30 && Countdown >= 20)
            {
                likelihood = _random.Next(200, 1000);
            }
            else if (Countdown < 20 && Countdown >= 10)
            {
                likelihood = _random.Next(400, 1000);
            }
            else if (Countdown < 10 && Countdown >= 7)
            {
                likelihood = _random.Next(600, 1000);
            }
            else if(Countdown < 7)
            {
                likelihood = _random.Next(800, 1000);
            }



            return ResponseByNumber(likelihood);
        }

        public string ResponsePicker(int num)
        {
            return ResponseByNumber(num);
        }

        private string ResponseByNumber(int likelihood)
        {
            try
            {
                dynamic responses = readJson();
                if (likelihood < 500)
                {
                    return "";
                }
                else if (likelihood >= 500 && likelihood < 700)
                {
                    var responseArr = responses.light;

                    int resp = _random.Next(responseArr.Count);
                    return responseArr[resp].text;
                }
                else if (likelihood >= 700 && likelihood < 850)
                {
                    var responseArr = responses.medium;
                    int resp = _random.Next(responseArr.length);
                    return responseArr[resp];
                }
                else if (likelihood >= 850)
                {
                    var responseArr = responses.heavy;
                    int resp = _random.Next(responseArr.length);
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
            using (StreamReader r = new StreamReader("../../../Controllers/Skynet.json"))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject(json);
            }
        }
    }
}
