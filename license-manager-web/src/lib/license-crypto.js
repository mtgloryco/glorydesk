import crypto from 'crypto';

const PRIVATE_KEY_B64 = process.env.LICENSE_PRIVATE_KEY;

export function signLicense(payload) {
    if (!PRIVATE_KEY_B64) {
        throw new Error('LICENSE_PRIVATE_KEY not found in environment');
    }

    const privateKey = Buffer.from(PRIVATE_KEY_B64, 'base64').toString('utf8');

    // Ensure stable serialization
    const payloadBuffer = Buffer.from(JSON.stringify(payload));

    // Sign using SHA-256
    const sign = crypto.createSign('SHA256');
    sign.update(payloadBuffer);
    sign.end();

    const signature = sign.sign(privateKey);

    const payloadBase64 = payloadBuffer.toString('base64');
    const signatureBase64 = signature.toString('base64');

    return `${payloadBase64}.${signatureBase64}`;
}

export function verifyLicense(licenseKey, publicKeyPem) {
    try {
        const [payloadBase64, signatureBase64] = licenseKey.split('.');
        const payloadBuffer = Buffer.from(payloadBase64, 'base64');
        const signatureBuffer = Buffer.from(signatureBase64, 'base64');

        const verify = crypto.createVerify('SHA256');
        verify.update(payloadBuffer);
        verify.end();

        return verify.verify(publicKeyPem, signatureBuffer);
    } catch (err) {
        return false;
    }
}
