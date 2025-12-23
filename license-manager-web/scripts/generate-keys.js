const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

function generateKeys() {
    console.log('Generating RSA-3072 key pair...');
    const { privateKey, publicKey } = crypto.generateKeyPairSync('rsa', {
        modulusLength: 3072,
        publicKeyEncoding: {
            type: 'spki',
            format: 'pem'
        },
        privateKeyEncoding: {
            type: 'pkcs8',
            format: 'pem'
        }
    });

    const envPath = path.join(__dirname, '../.env');
    let envContent = '';
    if (fs.existsSync(envPath)) {
        envContent = fs.readFileSync(envPath, 'utf8');
    }

    // Prepare content for .env - replace existing if they exist or append
    const privateKeyBase64 = Buffer.from(privateKey).toString('base64');
    const publicKeyBase64 = Buffer.from(publicKey).toString('base64');

    const privateKeyLine = `LICENSE_PRIVATE_KEY="${privateKeyBase64}"`;
    const publicKeyLine = `LICENSE_PUBLIC_KEY="${publicKeyBase64}"`;

    let newEnvContent = envContent;
    if (newEnvContent.includes('LICENSE_PRIVATE_KEY')) {
        newEnvContent = newEnvContent.replace(/LICENSE_PRIVATE_KEY=.*/, privateKeyLine);
    } else {
        newEnvContent += `\n${privateKeyLine}`;
    }

    if (newEnvContent.includes('LICENSE_PUBLIC_KEY')) {
        newEnvContent = newEnvContent.replace(/LICENSE_PUBLIC_KEY=.*/, publicKeyLine);
    } else {
        newEnvContent += `\n${publicKeyLine}`;
    }

    fs.writeFileSync(envPath, newEnvContent.trim() + '\n');

    console.log('Keys generated and saved to .env (Base64 encoded PEM)');
    console.log('\n--- PUBLIC KEY (PEM) ---');
    console.log(publicKey);
    console.log('------------------------');
    console.log('\nSAVE THIS PUBLIC KEY for your Avalonia Desktop App validation!');
}

generateKeys();
