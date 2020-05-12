using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.LotteryContract
{
    public partial class LotteryContract
    {
        public override Lottery GetLottery(Int64Value input)
        {
            return State.Lotteries[input.Value];
        }

        public override GetRewardResultOutput GetRewardResult(Int64Value input)
        {
            var period = State.Periods[input.Value];
            var rewardIds = period?.RewardIds;
            if (rewardIds == null || !rewardIds.Any())
            {
                return new GetRewardResultOutput();
            }

            // ReSharper disable once PossibleNullReferenceException
            var randomHash = period.RandomHash;
            // ReSharper disable once AssignNullToNotNullAttribute
            var lotteries = rewardIds.Select(id => State.Lotteries[id] ?? new Lottery()).ToList();

            return new GetRewardResultOutput
            {
                Period = input.Value,
                RandomHash = randomHash,
                RewardLotteries = {lotteries}
            };
        }

        public override GetBoughtLotteriesOutput GetBoughtLotteries(GetBoughtLotteriesInput input)
        {
            List<long> returnLotteryIds;
            var owner = input.Owner ?? Context.Sender;
            var lotteryList = State.OwnerToLotteries[owner][input.Period];
            if (lotteryList == null)
            {
                return new GetBoughtLotteriesOutput();
            }

            var allLotteryIds = lotteryList.Ids.ToList();
            if (allLotteryIds.Count <= MaximumReturnAmount)
            {
                returnLotteryIds = allLotteryIds;
            }
            else
            {
                Assert(input.StartIndex < allLotteryIds.Count, "Invalid start index.");
                var takeAmount = Math.Min(allLotteryIds.Count.Sub(input.StartIndex), MaximumReturnAmount);
                returnLotteryIds = allLotteryIds.Skip(input.StartIndex).Take((int) takeAmount).ToList();
            }

            return new GetBoughtLotteriesOutput
            {
                Lotteries =
                {
                    returnLotteryIds.Select(id => State.Lotteries[id] ?? new Lottery())
                }
            };
        }

        public override Int64Value GetSales(Int64Value input)
        {
            var period = State.Periods[input.Value];
            Assert(period != null, "Period information not found.");
            if (State.CurrentPeriod.Value == input.Value)
            {
                return new Int64Value
                {
                    // ReSharper disable once PossibleNullReferenceException
                    Value = State.SelfIncreasingIdForLottery.Value.Sub(period.StartId)
                };
            }

            var nextPeriod = State.Periods[input.Value.Add(1)];
            return new Int64Value
            {
                // ReSharper disable once PossibleNullReferenceException
                Value = nextPeriod.StartId.Sub(period.StartId)
            };
        }

        public override Int64Value GetPrice(Empty input)
        {
            return new Int64Value {Value = State.Price.Value};
        }

        public override Int64Value GetDrawingLag(Empty input)
        {
            return new Int64Value {Value = State.DrawingLag.Value};
        }

        public override Int64Value GetMaximumBuyAmount(Empty input)
        {
            return new Int64Value {Value = State.MaximumAmount.Value};
        }

        public override Int64Value GetCurrentPeriodNumber(Empty input)
        {
            return new Int64Value {Value = State.CurrentPeriod.Value};
        }

        public override PeriodBody GetPeriod(Int64Value input)
        {
            var period = State.Periods[input.Value];
            return period ?? new PeriodBody();
        }

        public override PeriodBody GetCurrentPeriod(Empty input)
        {
            var period = State.Periods[State.CurrentPeriod.Value];
            return period ?? new PeriodBody();
        }
    }
}