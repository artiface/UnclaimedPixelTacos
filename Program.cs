using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.ABI;

namespace ReadVault;

internal class UnclaimedPixelTacos
{
    static async Task Main(string[] args)
    {
        //set the staking contract address
        const string pixelTacoContract = "0x577eFe0525c83D2Bf2f8e9EfB1e41bA3FcB84c86";
        
        //max token id for taco tribe, but better to set the current max since we have to check each one
        //if you really wanted you could read if from the TacoTribe contract, but easy enough just to hard code it before you run 
        int maxTacoId = 3084; //8226; 

        //a map that will contain all the token id's and if they have minted a pixel or not
        var tokenIdToMintedMap = new Dictionary<int, bool>();

        //generate a private key
        //you don't really need a key to use read functions but the web3 object needs a key so just make a new one
        var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
        var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();

        //use the official RPC (or change to a private one)
        var rpc = "https://polygon-rpc.com";
        //make an account object from the private key and polygon chain
        var account = new Account(privateKey, 137);
        //make a web 3 instance from the account and the rpc
        var web3Poly = new Web3(account, rpc);

        //this is a little complicated because we have to read the private storage
        //since the mapping is declared private :(
        //mapping(uint256 => bool) private tokensMinted;

        //but we can follow this guide for reading protected mappings 
        // https://medium.com/@dariusdev/how-to-read-ethereum-contract-storage-44252c8af925
        // https://ethereum.stackexchange.com/questions/103461/reading-private-variable-of-mappingaddress-uint256-using-getstorageat
        // https://ethereum.stackexchange.com/questions/63930/how-do-i-use-soliditysha3-in-nethereum
        
        //this was tough, but the mapping exists at index 10 (found by trial and error because all the included contract have some privates too)
        var mappingIndex = new HexBigInteger(10);

        for (var k = 1; k <= maxTacoId; k++)
        {
            var key = new HexBigInteger(k);

            //our address is going to be the key + the mapping index - make a long hex string of it
            //each one is 32 bytes, 64 hex characters so pad to make sure we align everything properly on the words
            var keyPreImage = "0x" + key.ToHexByteArray().ToHex().PadLeft(64, '0') +  mappingIndex.ToHexByteArray().ToHex().PadLeft(64, '0');
            //Console.WriteLine($"key pre: {keyPreImage}");
            
            //encode the key 
            var abiEncode = new ABIEncode();
            var hashedKey = abiEncode.GetSha3ABIEncodedPacked(keyPreImage.HexToByteArray());          
            //Console.WriteLine($"hashed: {hashedKey.ToHex()}");

            var result = await web3Poly.Eth.GetStorageAt
                .SendRequestAsync(pixelTacoContract, new HexBigInteger(hashedKey.ToHex()));
            
            //it's claimed if the result is > 1
            bool claimed = new HexBigInteger(result).Value > 0 ;

            //show progress
            Console.WriteLine($"Token {k} - {claimed}");

            //add them to our map
            tokenIdToMintedMap.Add(k, claimed);
        }
        
        //save them to files
        using (StreamWriter file = new StreamWriter("Claimed.txt"))
        foreach (var entry in tokenIdToMintedMap.Where(kvp => kvp.Value == true))
            file.WriteLine("Token: {0}", entry.Key); 
        
        using (StreamWriter file = new StreamWriter("Unclaimed.txt"))
        foreach (var entry in tokenIdToMintedMap.Where(kvp => kvp.Value == false))
            file.WriteLine("Token: {0}", entry.Key); 
    }

}