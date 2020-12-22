using Cryptography.Obfuscation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Survey123EmailNotification.Helpers
{
    public class IdObfuscator
    {
        Obfuscator obfuscator = new Obfuscator();
        int offest = 100000;
        public IdObfuscator()
        {
            obfuscator.Seed = 1512544541;

        }

        public string encryptId(int originalFeatureId)
        {
            int featureId = originalFeatureId + offest;
            return obfuscator.Obfuscate(featureId);
        }

        public int decryptId(string encyptedFeatureId)
        {
            int featureId = obfuscator.Deobfuscate(encyptedFeatureId);
            return featureId - offest;
        }

    }
}
