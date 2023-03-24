using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

public class TOTP : MonoBehaviour
{

    SHA256 hashingAlgorithm;
    long timeInterval = (60 /*Change this to change reset interval*/)*10000000L; //In 100 nanosecond increments, equals 60 seconds. 
    int backwardsChecks = 2;
    int forwardsChecks = 4;

    //string[] segments = {".","-","..",".-","-.","--","...","..-",".-.",".--","-..","-.-","--.","---","....","...-","..-.","..--",".-..",".-.-",".--.",".---","-...","-..-","-.-.","-.--","--..","--.-","---.","----"};
    string[] segments = { "..", ".-", "-.", "--", "...", "..-", ".-.", ".--", "-..", "-.-", "--.", "...-", "..-.", "..--", ".-..", ".-.-", ".--.", "-...", "-..-", "-.-.", "-.--", "--.."};

    byte[] primaryAccountSecret = {0,0,0,0,0,0,0,0,
                                    164,250,78,53,177,183,209,55,136,39,
                                    18,220,194,13,206,65,75,81,33,122,79,
                                    124,34,253,148,151,106,229,150,172,
                                    120,158,237,59,221,112,174,28,154,
                                    54,31,40,116,184,132,38,193,61,7,74,
                                    69,64,35,123,50,215,159,125,133,88,105,
                                    204,9,87,96,189,44,49,51,239,201,62,95,
                                    227,127,228,68,225,211,210,207,161,73,
                                    0,36,170,52,109,107,180,169,140,216,
                                    128,90,241,187,197,238,178,101,254,142,
                                    121,231,185,135,43,110,19,66,83,226,160,
                                    181,244,240,17,129,173,1,156,42,117,
                                    16,141,5,111};

    

    byte[] emulatedSecret;
    string emulatedUserName = "random";
    byte[] loginUserSecret;

    // Start is called before the first frame update
    void Start()
    {
        hashingAlgorithm = SHA256.Create();
        emulatedSecret = primaryAccountSecret;
        setLoginUser("random");
    }

    // Update is called once per frame
    void Update()
    {
    }

    string computeCode(byte[] secret) {
        long time = System.DateTime.UtcNow.ToFileTime();  //# of 100nanosecond increments since 1601CE
        time = time - time % timeInterval;  //Convert to multiple of time interval
        //Debug.Log("Time:" + time);
        return computeCode(secret, time);;
    }

    string computeCode(byte[] secret, long time) {
        
        byte[] secret_stamped = (byte[])secret.Clone();

        //Add time to secret array
        for(int i = 0; i<8; i++) {
            secret_stamped[i] = (byte)time;
            time = time>>8;
        }

        byte[] hash = hashingAlgorithm.ComputeHash(secret_stamped);
        Debug.Assert(hash.Length >= 32, "Hashing algorithm did not work as expected!");

        string code = "";

        for(int i = 0; i < 4*4; i+=4) {
            long segmentIndex = 0;

            for(int j=0; j<4; j++) {
                segmentIndex += ((long)hash[i+j])<<(8*j);
            }

            segmentIndex = segmentIndex%segments.Length;

            code = code + segments[segmentIndex] + " ";
        }

        return code.Substring(0,code.Length-1);
    }

    bool codeIsValid(byte[] secret, string code) {
        
        long time = System.DateTime.UtcNow.ToFileTime();  //# of 100nanosecond increments since 1601CE
        time = time - time % timeInterval;  //Convert to multiple of time interval

        for(int i = -backwardsChecks; i<forwardsChecks+1; i++) {
            if(code.Equals(computeCode(secret, time+i*timeInterval))) {
                return true;
            }
        }
        
        return false;
    }

    public string generateEmulatedCode() {
        return computeCode(emulatedSecret);
    }

    public bool checkCode(string code) {
        return codeIsValid(loginUserSecret, code);
    }

    public void setLoginUser(string userName) {
        if(userName.Equals(emulatedUserName)) {
            //Set secret to the emulated secret
            loginUserSecret = primaryAccountSecret;
        }else{
            //generate a unique secret for other users (would normally be randomly generated once, then stored in a secure database)
            byte[] hash = hashingAlgorithm.ComputeHash(System.Text.Encoding.Unicode.GetBytes(userName));
            byte[] secret = new byte[128+8];
            for(int i=0; i<32; i++) {
                secret[i+8] = hash[i];
            }
            loginUserSecret = secret;
        }
    }
    
}
