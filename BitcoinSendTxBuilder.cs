using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json;
 
public class BitcoinSendTxBuilder
{
    Network network;

    public BitcoinSendTxBuilder(Network network)
    {
        this.network = network;
    }

    public async Task<string> CreateSignedSendCoinTx(
        BitcoinAddress senderAddress,
        BitcoinAddress destinationAddress, 
        Money amountToSend, 
        Money feeLevel, 
        BitcoinSecret secret,
        List<UTXO> utxos)
    { 
        BitstreamBitcoinClient client = new BitstreamBitcoinClient(network);
        FeeRate feeRate = new FeeRate(Money.Satoshis(feeLevel), 1);

        // List<UTXO> utxos = await GetUTXOsAsync(senderAddress);
        if (utxos == null || utxos.Count == 0)
        {
            Console.WriteLine("No UTXOs found.");
            return null;
        }

        var builder = network.CreateTransactionBuilder();
        var feesAmount = feeRate.GetFee(250);

        // sort the utxos by value
        utxos = utxos.OrderByDescending(utxo => utxo.Value).ToList();

        // sum the utxos.Value for total available
        // long totalAvailable = 0;
        List<Coin> allCoins = new List<Coin>();
        long totalAvailable = 0;
        foreach (UTXO utxo in utxos)
        {
            allCoins.Add(await client.FetchUTXO(utxo.TxId, utxo.OutputIndex, network));
            totalAvailable += utxo.Value;
            if(totalAvailable>=amountToSend.Satoshi+feesAmount){
                break;
            }
        }

        // check if no coins were fetched
        if (allCoins.Count == 0)
        {
            Console.WriteLine("No UTXOs found.");
            return null;
        }

        // check if the totalAvailable is less than the amountToSend
        if (totalAvailable < amountToSend.Satoshi + feesAmount.Satoshi)
        {
            Console.WriteLine("Insufficient funds.");
            return null;
        }

        var signedTransaction = builder
            .AddCoins(allCoins)
            .AddKeys(secret.PrivateKey)
            .Send(destinationAddress, amountToSend)
            .SetChange(senderAddress) // Ensure this is the change address
            .SendFees(feesAmount)
            .BuildTransaction(true);

        // sign the transaction
        // NBitcoin.Transaction signedTransaction = builder.SignTransaction(transaction);

        // return signedTransaction.ToHex();

        // Ensure the transaction is correctly built
        if (!builder.Verify(signedTransaction, out var errors))
        {
            Console.WriteLine("Verification failed: " + errors);
            return null;
        }
        else
        {
            Console.WriteLine("Transaction verified successfully.");
            // broadcast the transaction
            string transactionHex = signedTransaction.ToHex();

            return transactionHex;
        }
    }
}
