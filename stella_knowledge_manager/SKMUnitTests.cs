using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace stella_knowledge_manager
{
    public class SKMUnitTests
    {
        [Fact]
        public void AddingDefaultAppsForFileTypes()
        {

        }

        /// <summary>
        /// Right now this can be really hard to test and does not work as intendend
        /// </summary>
        [Fact]
        public void LearningShouldOpenFileWithDefaultApp()
        {
            SKM skm = new SKM();
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, "TESTING");
            File.Create(appDataFolder + "/Opentest.txt");

            skm.AddItem("NAME", "DESCRIPTION", appDataFolder + "test.txt", 0);
            skm.SaveData("TESTING");


            //skm.StartLearning();

            // Clean Up
            Directory.Delete(myAppDataFolder, true);
        }

        [Fact]
        public void SavingData()
        {
            SKM skm = new SKM();
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, "TESTING");
            File.Create(appDataFolder + "/Savetest.txt");

            skm.AddItem("NAME", "DESCRIPTION", appDataFolder + "/test.txt", 0);
            skm.SaveData("TESTING");


            Assert.Equal("NAME", skm.GetItemByName("NAME").Name);

            // Clean Up
            Directory.Delete(myAppDataFolder, true);
        }

        [Fact]
        public void LoadingData()
        {
            SKM skm = new SKM();
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, "TESTING");
            File.Create(appDataFolder + "/Loadtest.txt");

            skm.AddItem("NAME", "DESCRIPTION", appDataFolder + "/test.txt", 0);
            skm.SaveData("TESTING");


            skm = new SKM();
            skm.LoadData("TESTING");

            Assert.Equal("NAME", skm.GetItemByName("NAME").Name);

            // Clean Up
            Directory.Delete(myAppDataFolder, true);
        }
    }
}
