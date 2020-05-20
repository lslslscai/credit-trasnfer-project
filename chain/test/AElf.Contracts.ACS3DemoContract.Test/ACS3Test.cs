using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Token;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using TimestampHelper = AElf.Kernel.TimestampHelper;

namespace AElf.Contracts.ACS3DemoContract
{
    public class ACS3Test : ACS3DemoContractTestBase
    {
        [Fact]
        public async Task Test()
        {
            var keyPair = SampleECKeyPairs.KeyPairs[0];
            var acs3DemoContractStub =
                GetTester<ACS3DemoContractContainer.ACS3DemoContractStub>(DAppContractAddress, keyPair);
            var tokenContractStub =
                GetTester<TokenContractContainer.TokenContractStub>(
                    GetAddress(TokenSmartContractAddressNameProvider.StringName), keyPair);

            var proposalId = (await acs3DemoContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = nameof(acs3DemoContractStub.SetSlogan),
                ToAddress = DAppContractAddress,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = new StringValue {Value = "AElf"}.ToByteString(),
                Token = HashHelper.ComputeFrom("AElf")
            })).Output;

            await tokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = DAppContractAddress,
                Symbol = "ELF",
                Amount = long.MaxValue
            });

            await acs3DemoContractStub.Approve.SendAsync(proposalId);
            
            // Check slogan
            {
                var slogan = await acs3DemoContractStub.GetSlogan.CallAsync(new Empty());
                slogan.Value.ShouldBeEmpty();
            }

            await acs3DemoContractStub.Release.SendAsync(proposalId);
            
            // Check slogan
            {
                var slogan = await acs3DemoContractStub.GetSlogan.CallAsync(new Empty());
                slogan.Value.ShouldBe("AElf");
            }
        }
    }
}