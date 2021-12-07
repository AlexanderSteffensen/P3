using System;
using System.Linq;
using System.Threading.Tasks;
using BlazorApp.Data;
using DataStreamingSimulation;
using ExecuteSQLScript;
using Xunit;
using Xunit.Abstractions;

namespace P3ConversionDashboard.Tests.BlazorBackendTest
{
    [Collection("Sequential")]
    public class BlazorBackendIntegrationTest
    {

        private DatabaseStreamer testDatabaseStreamer;
        
        private readonly ITestOutputHelper _testOutputHelper;

        public BlazorBackendIntegrationTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            
        }

        [Fact]
        public async void BlazorBackendTest()
        {
            System.Threading.Thread.Sleep(1000);
            await Task.Run(() => SQLScriptExecuter.CreateDB("../../../BlazorBackendTest/DROP_ANS_DB_P3_TEST.sql"));
            await Task.Run(() => SQLScriptExecuter.CreateDB("../../../BlazorBackendTest/NEW_CREATE_ANS_DB_P3_TEST.sql"));
            
            ConversionDataAssigner.Start("../../../BlazorBackendTest/setupBlazorBackend.txt");

            testDatabaseStreamer = new DatabaseStreamer("../../../BlazorBackendTest/setupDataStreamingBlazorTest.txt", 
                "2021-10-28 15:07:10.347","2021-10-28 15:09:50.533");
            
            testDatabaseStreamer.Stream(100);

            TimeSpan ts = DateTime.Parse("2021-10-28 15:09:50.533") - DateTime.Parse("2021-10-28 15:07:10.347");
            
            System.Threading.Thread.Sleep((int) ts.TotalMilliseconds / 10 + 1000);

            CheckData();
        }

        public void CheckData()
        {
            _testOutputHelper.WriteLine(ConversionDataAssigner.FinishedManagers.Count.ToString());
            for (int i = 0; i < BlazorBackendCheckData.testManagers.Count; i++)
            {
                ManagerStatusHandler manager = ConversionDataAssigner.FinishedManagers[i];
                TestManagerStatusHandler testManager = BlazorBackendCheckData.testManagers[i];
                
                //checks manager name
                Assert.Equal(testManager.managerName, manager.Name);

                if (testManager.CPU != null)
                {
                    //checks CPU
                    Assert.Contains(testManager.CPU.LogTime, manager.Health.Cpu.Select(data => data.LogTime));
                    Assert.Contains(testManager.CPU.NumericValue, manager.Health.Cpu.Select(data => data.NumericValue));
                    Assert.Contains(testManager.CPU.ReportType, manager.Health.Cpu.Select(data => data.ReportType));

                    //checks Memory
                    Assert.Contains(testManager.Memory.LogTime, manager.Health.Memory.Select(data => data.LogTime));
                    Assert.Contains(testManager.Memory.NumericValue, manager.Health.Memory.Select(data => data.NumericValue));
                    Assert.Contains(testManager.Memory.ReportType, manager.Health.Memory.Select(data => data.ReportType));
                }
                
                //checks Error
                foreach (LogData data in manager.ErrorHandler.LogDataList)
                {
                    _testOutputHelper.WriteLine(data.Description);
                }
                _testOutputHelper.WriteLine(manager.ErrorHandler.LogDataList.Count.ToString());
                _testOutputHelper.WriteLine("");
                Assert.Contains(testManager.Error.Description, manager.ErrorHandler.LogDataList.Select(data => data.Description));
                Assert.Contains(testManager.Error.Grade, manager.ErrorHandler.LogDataList.Select(data => data.Grade));
                Assert.Contains(testManager.Error.Timestamp, manager.ErrorHandler.LogDataList.Select(data => data.Timestamp));
                Assert.Contains(testManager.Error.ManagerName, manager.ErrorHandler.LogDataList.Select(data => data.ManagerName));
                
                //checks Reconciliations
                if (testManager.Reconciliation != null)
                {
                    _testOutputHelper.WriteLine(manager.ReconciliationHandler.LogDataList.Select(data => data.Description).Count().ToString());
                    Assert.Contains(testManager.Reconciliation.Description, manager.ReconciliationHandler.LogDataList.Select(data => data.Description));
                    Assert.Contains(testManager.Reconciliation.Grade, manager.ReconciliationHandler.LogDataList.Select(data => data.Grade));
                    Assert.Contains(testManager.Reconciliation.Timestamp, manager.ReconciliationHandler.LogDataList.Select(data => data.Timestamp));
                    Assert.Contains(testManager.Reconciliation.ManagerName, manager.ReconciliationHandler.LogDataList.Select(data => data.ManagerName));
                }
            }
        }
    }
}