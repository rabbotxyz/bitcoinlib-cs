using NBitcoin;
using CommandLine;

class Program
{
    

    public class Options
    {
        [Option('s', "seed", Required = true, HelpText = "The seed phrase to use.")]
        public string Seed { get; set; }

        [Option('n', "network", Required = true, HelpText = "The network to use.")]
        public string Network { get; set; }

        [Option('r', "recipient", Required = true, HelpText = "The recipient address.")]
        public string Recipient { get; set; }

        [Option('a', "amount", Required = true, HelpText = "The amount to send in SATs.")]
        public long Amount { get; set; }

        // fee level in sat/byte
        [Option('f', "fee", Required = false, HelpText = "The fee level in sat/byte.")]
        public long Fee { get; set; }
    }

    static Network NetworkFromString(string networkS) {
        Network network = Network.TestNet; // default to testnet
        if (networkS.ToLower() == "main")
        {
            network = Network.Main;
        } 
        else if (networkS.ToLower() == "testnet")
        {
            network = Network.TestNet;
        } 
        else if (networkS.ToLower() == "regtest")
        {
            network = Network.RegTest;
        } 
        return network;
    }

    static async Task Main(string[] args)
    {

        Network network = null;
        string seedphrase = null;
        string recipient = null;
        long amountToSend = 0;
        long feeLevel = 20; // default fee level

        Parser.Default.ParseArguments<Options>(args)
        .WithParsed<Options>(o =>
        {
            network = NetworkFromString(o.Network);
            seedphrase = o.Seed;
            recipient = o.Recipient;
            amountToSend = o.Amount;
            if (o.Fee > 0)
            {
                feeLevel = o.Fee;
            }
        });

        if (network == null || seedphrase == null || recipient == null || amountToSend == 0)
        {
            // exit if the network is not set
            Console.WriteLine("Required args not set.");
            return;
        }

        // isntatiate the bitcoin client
        BitstreamBitcoinClient client = new BitstreamBitcoinClient(network);
        
        // Generate a new mnemonic (seed phrase). You can also use an existing one.
        Mnemonic mnemonic = new Mnemonic(seedphrase, Wordlist.English);
        // Mnemonic mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
        Console.WriteLine($"Mnemonic: {mnemonic}");

        // Derive the master key from the mnemonic
        ExtKey masterKey = mnemonic.DeriveExtKey();
        Console.WriteLine($"Master Key: {masterKey}");

        // Get the root HD (Hierarchical Deterministic) key
        var root = new ExtKey().Derive(new KeyPath("m/84'/0'/0'"));

        // Deriving first few addresses
        long amountToSent = 0;
        for (int i = 0; i < 1; i++) // only the first address for now
        {
            var path = new KeyPath($"m/84'/0'/0'/0/{i}");
            var key = masterKey.Derive(path);
            var sendAddress = key.Neuter().PubKey.GetAddress(ScriptPubKeyType.Segwit, network);

            // get the utxos for the address
            List<UTXO> utxos = await client.GetAddressUTXOs($"{sendAddress}");

            // sum the utxos
            long balance = BitstreamBitcoinClient.SumUTXOs(utxos);
            Console.WriteLine($"Amount to send:{amountToSend} Balance:{balance} Fee Level:{feeLevel} sat/byte");

            if (balance > 0 && balance >= amountToSend)
            {
                // print the address and balance
                Console.WriteLine($"Address {sendAddress} Balance:{balance} SATs");
                BitcoinSecret secret = masterKey.Derive(path).PrivateKey.GetBitcoinSecret(network); // For Mainnet
                var recipientAddress = BitcoinAddress.Create(recipient, network);
                // var changeAmount = balance - amountToSend;
                // var feeLevel = 20; // 20 sat/byte

                BitcoinSendTxBuilder builder = new BitcoinSendTxBuilder(network);             
                string txHex = await builder.CreateSignedSendCoinTx(
                    sendAddress,
                    recipientAddress, Money.Satoshis(amountToSend), 
                    Money.Satoshis(feeLevel), secret, utxos);

                if (txHex == null)
                {
                    Console.WriteLine("Transaction failed.");
                    break;
                } else {
                    Console.WriteLine($"Unsigned Transaction Hex: {txHex}");

                    PrintTxInfo(txHex, network);

                    // send the transaction
                    string txId = await client.BroadcastTransactionRaw(txHex);
                    if (txId != null)
                    {
                        amountToSent = amountToSend;
                        Console.WriteLine($"Transaction ID: {txId}");
                    } else {
                        Console.WriteLine("Transaction failed.");
                    }
                    
                    break;
                }

            } else {
                Console.WriteLine($"Balance in the address: {sendAddress} is less than the amount to send.");
            
            }

            // // if the amount sent > 0 then break the loop
            // long zero_long = 0;
            // if (amountToSent > zero_long)
            // {   Console.WriteLine("Transaction successful.");
            //     break;
            // }
           
        }
    }

    static void PrintTxInfo(string txHex, Network network) {
        // Parse the transaction hex

        NBitcoin.Transaction tx = NBitcoin.Transaction.Parse(txHex, network);

        // Display the transaction details
        Console.WriteLine("Transaction ID: " + tx.GetHash());
        // total output value of the transaction
        Console.WriteLine("Total Output Value: " + tx.TotalOut.Satoshi);
        Console.WriteLine("Number of Inputs: " + tx.Inputs.Count);
        Console.WriteLine("Number of Outputs: " + tx.Outputs.Count);

        // Loop through each input
        foreach (TxIn input in tx.Inputs)
        {
            Console.WriteLine("Input Transaction ID: " + input.PrevOut.Hash);
            Console.WriteLine("Input Output Index: " + input.PrevOut.N);
        }

        // Loop through each output
        foreach (TxOut output in tx.Outputs)
        {
            Console.WriteLine("Output Value: " + output.Value.Satoshi);
            Console.WriteLine("Output ScriptPubKey: " + output.ScriptPubKey);
        }
    }
}