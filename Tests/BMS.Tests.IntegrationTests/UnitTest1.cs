using System.Text.Json;
using System.Text;
using Xunit.Abstractions;
using BMS.Tests.IntegrationTests.Contracts;

namespace BMS.Tests.IntegrationTests
{
    public class IntegrationTest
    {
        private readonly HttpClient _httpClient;
        private readonly ISignalRWrapper _signalR;
        private readonly ITestOutputHelper _testOutputHelper;

        public IntegrationTest(IHttpClientFactory httpClientFactory, ISignalRWrapperFactory signalRWrapperFactory, ITestOutputHelper testOutputHelper)
        {
            _signalR = signalRWrapperFactory.Create(testOutputHelper);
            _testOutputHelper = testOutputHelper;
            _httpClient = httpClientFactory.CreateClient("IntegrationTest");


        }
        [Fact]
        public async Task TestRegisterAccount()
        {
            var customerRegistrationInfo = new CustomerRegistrationInfo()
            {
                CallerId = "Teller1",
                RequestId = Guid.NewGuid().ToString(),
                SchemaVersion = "1.0",
                FirstName = "John",
                LastName = "Doe",
                Address = "123 Main St",
                City = "Any Town",
                State = "Any State",
                ZipCode = "90210",
                Email = "john@company.com",
                PhoneNumber = "+1-555-555-5555",
            };
                    
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(customerRegistrationInfo, serializeOptions);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            await _signalR.StartSignalR();
            var response = await _httpClient.PostAsync("RegisterCustomer", data);
            Assert.NotNull(response);
            Assert.Equal(200, (int)response.StatusCode);

            var result = await _signalR.WaitForSignalREventAsync();
            Assert.True(result);
            Assert.NotEmpty(_signalR.Messages);
            //Assert.Contains(customerInfo.AccountId, _signalR.Messages.Select(e => e.AccountId));

            var testResponse = await _httpClient.GetAsync($"GetAccountId?email={customerRegistrationInfo.Email}");
            Assert.NotNull(testResponse);
            Assert.Equal(200, (int)response.StatusCode);
            var responseJson = await testResponse.Content.ReadAsStringAsync();


            var accountArray = JsonSerializer.Deserialize<AccountIdInfo>(responseJson, serializeOptions);
            Assert.NotNull(accountArray);
            Assert.NotEmpty(accountArray.AccountIds);

            Assert.Contains(customerRegistrationInfo.RequestId, accountArray.AccountIds);
           
        }

    }
}