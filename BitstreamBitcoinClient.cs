using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NBitcoin;
using System.Text;

public class BlockstreamAddressInfo
{
    [JsonProperty("address")]
    public string Address { get; set; }

    [JsonProperty("chain_stats")]
    public ChainStats ChainStats { get; set; }

    [JsonProperty("mempool_stats")]
    public MempoolStats MempoolStats { get; set; }
}

public class ChainStats
{
    [JsonProperty("funded_txo_count")]
    public int FundedTxoCount { get; set; }

    [JsonProperty("funded_txo_sum")]
    public long FundedTxoSum { get; set; }

    [JsonProperty("spent_txo_count")]
    public int SpentTxoCount { get; set; }

    [JsonProperty("spent_txo_sum")]
    public long SpentTxoSum { get; set; }

    [JsonProperty("tx_count")]
    public int TxCount { get; set; }
}

public class MempoolStats
{
    [JsonProperty("funded_txo_count")]
    public int FundedTxoCount { get; set; }

    [JsonProperty("funded_txo_sum")]
    public long FundedTxoSum { get; set; }

    [JsonProperty("spent_txo_count")]
    public int SpentTxoCount { get; set; }

    [JsonProperty("spent_txo_sum")]
    public long SpentTxoSum { get; set; }

    [JsonProperty("tx_count")]
    public int TxCount { get; set; }
}


public class UTXO
{
    [JsonProperty("txid")]
    public string TxId { get; set; }

    [JsonProperty("vout")]
    public int Vout { get; set; }

    [JsonProperty("status")]
    public Status Status { get; set; }

    [JsonProperty("value")]
    public long Value { get; set; }

    // set by the client
    public Transaction Transaction { get; set; }

    public int OutputIndex { get; set; }

    public string ScriptPubKey { get; set; }
}

public class Status
{
    [JsonProperty("confirmed")]
    public bool Confirmed { get; set; }

    [JsonProperty("block_height")]
    public int BlockHeight { get; set; }

    [JsonProperty("block_hash")]
    public string BlockHash { get; set; }

    [JsonProperty("block_time")]
    public int BlockTime { get; set; }
}

public class Transaction {
    [JsonProperty("txid")]
    public string TxId { get; set; }

    [JsonProperty("version")]
    public int Version { get; set; }

    [JsonProperty("locktime")]
    public int LockTime { get; set; }

    [JsonProperty("vin")]
    public List<Vin> Vins { get; set; }

    [JsonProperty("vout")]
    public List<Vout> Vouts { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("weight")]
    public int Weight { get; set; }

    [JsonProperty("fee")]
    public long Fee { get; set; }

    [JsonProperty("status")]
    public Status Status { get; set; }
}

public class Vin {
    [JsonProperty("txid")]
    public string TxId { get; set; }

    [JsonProperty("vout")]
    public int Vout { get; set; }

    [JsonProperty("prevout")]
    public Prevout Prevout { get; set; }

    [JsonProperty("scriptsig")]
    public string ScriptSig { get; set; }

    [JsonProperty("scriptsig_asm")]
    public string ScriptSigAsm { get; set; }

    [JsonProperty("witness")]
    public List<string> Witness { get; set; }

    [JsonProperty("is_coinbase")]
    public bool IsCoinbase { get; set; }

    [JsonProperty("sequence")]
    public long Sequence { get; set; }
}

public class Vout {
    [JsonProperty("scriptpubkey")]
    public string ScriptPubKey { get; set; }

    [JsonProperty("scriptpubkey_asm")]
    public string ScriptPubKeyAsm { get; set; }

    [JsonProperty("scriptpubkey_type")]
    public string ScriptPubKeyType { get; set; }

    [JsonProperty("scriptpubkey_address")]
    public string ScriptPubKeyAddress { get; set; }

    [JsonProperty("value")]
    public long Value { get; set; }
}

public class Prevout {
    [JsonProperty("scriptpubkey")]
    public string ScriptPubKey { get; set; }

    [JsonProperty("scriptpubkey_asm")]
    public string ScriptPubKeyAsm { get; set; }

    [JsonProperty("scriptpubkey_type")]
    public string ScriptPubKeyType { get; set; }

    [JsonProperty("scriptpubkey_address")]
    public string ScriptPubKeyAddress { get; set; }

    [JsonProperty("value")]
    public long Value { get; set; }
}


public class BitstreamBitcoinClient
{

    private Network network;

    // the constructor
    public BitstreamBitcoinClient(NBitcoin.Network network)
    {
        this.network = network;
    }

   
    public async Task<BlockstreamAddressInfo> GetAddressInfo(string address)
    {

        string url = $"https://blockstream.info/api/address/{address}";
        if (this.network == Network.TestNet)
        {
            url = $"https://blockstream.info/testnet/api/address/{address}";
        }
        else if (this.network == Network.RegTest)
        {
            url = $"https://blockstream.info/regtest/api/address/{address}";
        }
        else if (this.network == Network.Main)
        {
            url = $"https://blockstream.info/{network}/api/address/{address}";
        }


        using (var httpClient = new HttpClient())
        {
            try
            {
                string jsonResponse = await httpClient.GetStringAsync(url);
                var response = JsonConvert.DeserializeObject<BlockstreamAddressInfo>(jsonResponse);

                if (response != null)
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return null;
        }
    }


    public async Task<List<UTXO>> GetAddressUTXOs(string address){
        string url = $"https://blockstream.info/api/address/{address}/utxo";
        if (this.network == Network.TestNet)
        {
            url = $"https://blockstream.info/testnet/api/address/{address}/utxo";
        }
        else if (this.network == Network.RegTest)
        {
            url = $"https://blockstream.info/regtest/api/address/{address}/utxo";
        }
        else if (this.network == Network.Main)
        {
            url = $"https://blockstream.info/api/address/{address}/utxo";
        }

        using (var httpClient = new HttpClient())
        {
            try
            {
                string jsonResponse = await httpClient.GetStringAsync(url);
                var response = JsonConvert.DeserializeObject<List<UTXO>>(jsonResponse);

                if (response != null)
                {
                    // int index = 0;
                    foreach (UTXO utxo in response)
                    {
                        // set the output index this might also be the utxo.Vout
                        utxo.OutputIndex = utxo.Vout;
                        utxo.Transaction = await GetTransaction(utxo.TxId);
                        utxo.ScriptPubKey = utxo.Transaction.Vouts[utxo.Vout].ScriptPubKey;
                    }

                    // filter out utxos where the 

                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return null;
        }
    }

    public static long SumUTXOs(List<UTXO> utxos){
        long sum = 0;
        foreach (UTXO utxo in utxos)
        {
            // if status is confirmed
            if (utxo.Status.Confirmed == true) {
                sum += utxo.Value;
            }
        }
        return sum;
    }

    public async Task<Coin> FetchUTXO(string txId, int outputIndex, Network network)
    {
        try
        {

            string url = $"https://blockstream.info/api/tx/{txId}";
            if (this.network == Network.TestNet)
            {
                url = $"https://blockstream.info/testnet/api/tx/{txId}";
            }
            else if (this.network == Network.RegTest)
            {
                url = $"https://blockstream.info/regtest/api/tx/{txId}";
            }
            else if (this.network == Network.Main)
            {
                url = $"https://blockstream.info/api/tx/{txId}";
            }

            using (var httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();

                JObject txData = JObject.Parse(json);
                JArray outputs = (JArray)txData["vout"];

                if (outputIndex < outputs.Count)
                {
                    JObject output = (JObject)outputs[outputIndex];
                    // Console.WriteLine(output.ToString());
                    long satoshis = (long)output["value"] ;  // Convert bitcoins to satoshis
                    string scriptHex = output["scriptpubkey"].ToString();

                    return new Coin(new OutPoint(uint256.Parse(txId), (uint)outputIndex),
                                    new TxOut(new Money(satoshis), Script.FromHex(scriptHex)));
                }
                return null;

            }
            
        }
        catch (Exception e)
        {
            Console.WriteLine("Error fetching UTXO: " + e.Message);
            return null;
        }
    }

    static async Task<Coin> FetchPrevOut(uint256 hash, int outputIndex, Network network)
    {
        try
        {
            string url = $"https://blockstream.info/api/tx/{hash}/hex";

  
            if (network == Network.TestNet)
            {
                url = $"https://blockstream.info/testnet/api/tx/{hash}/hex";
            }
            else if (network == Network.RegTest)
            {
                url = $"https://blockstream.info/regtest/api/tx/{hash}/hex";
            }
            else if (network == Network.Main)
            {
                url = $"https://blockstream.info/api/tx/{hash}/hex";
            }

            using (var httpClient = new HttpClient())
            { 

                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string txHex = await response.Content.ReadAsStringAsync();

                NBitcoin.Transaction prevTx = NBitcoin.Transaction.Parse(txHex, network);
                TxOut prevTxOut = prevTx.Outputs[outputIndex];

                return new Coin(prevTx, (uint)outputIndex);
            }




        }
        catch (Exception e)
        {
            Console.WriteLine("Error fetching previous transaction: " + e.Message);
            return null;
        }
    }

    public async Task<Transaction> GetTransaction(string txid) {

        //https://blockstream.info/api/tx/0056283b9adc1b4067a20c77915b3618cc81c1b01215ce071a9e783cf5c406b1
        string url = $"https://blockstream.info/api/tx/{txid}";
        if (this.network == Network.TestNet)
        {
            url = $"https://blockstream.info/testnet/api/tx/{txid}";
        }
        else if (this.network == Network.RegTest)
        {
            url = $"https://blockstream.info/regtest/api/tx/{txid}";
        }
        else if (this.network == Network.Main)
        {
            url = $"https://blockstream.info/api/tx/{txid}";
        }

        using (var httpClient = new HttpClient())
        {
            try
            {
                string jsonResponse = await httpClient.GetStringAsync(url);
                var response = JsonConvert.DeserializeObject<Transaction>(jsonResponse);

                if (response != null)
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return null;
        }

    }


    public async Task<string> BroadcastTransactionRaw(string signedHex)
    {
        var url = "https://blockstream.info/testnet/api/tx";  // Change to mainnet URL if necessary
        if (this.network == Network.TestNet)
        {
            url = $"https://blockstream.info/testnet/api/tx";
        }
        else if (this.network == Network.RegTest)
        {
            url = $"https://blockstream.info/regtest/api/tx";
        }
        else if (this.network == Network.Main)
        {
            url = $"https://blockstream.info/api/tx";
        }


        using (var client = new HttpClient())
        {
            // var content = new StringContent($"\"{signedHex}\"", Encoding.UTF8, "application/json");
            var content = new StringContent(signedHex, Encoding.UTF8, "text/plain");
            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseString);
            return responseString;
        }
    }



}
