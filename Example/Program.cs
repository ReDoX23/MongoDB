using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    public class User:IBaseClass
    {
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
    }
    class Program
    {
        static void Main(string[] args)
        {
            User user = new User();
            user.Name = "Resul";
            user.Surname = "DOĞAN";
            user.Insert();

            user.Name = "ReDoX";
            UpdateResult result= user.Update();
            Console.WriteLine(result.ModifiedCount + " row afffected");

            
            List<User> users = user.GetbyAtribute<User>("Name", "ReDoX");
            foreach (User us in users)
                Console.WriteLine(us.Name + " " + us.Surname);

            User user2 = new User();
            //user=user2
            user2.Fill<User>(user._id);
            
        }
    }
}
