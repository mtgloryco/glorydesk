import nodemailer from 'nodemailer';

const transporter = nodemailer.createTransport({
    host: process.env.SMTP_HOST || 'smtp.gmail.com',
    port: parseInt(process.env.SMTP_PORT || '587'),
    secure: process.env.SMTP_SECURE === 'true',
    auth: {
        user: process.env.SMTP_USER,
        pass: process.env.SMTP_PASS,
    },
});

export async function sendLicenseActivationEmail(to, username, licenseKey, tier) {
    if (!process.env.SMTP_USER || !process.env.SMTP_PASS) {
        console.warn('Email skipped: SMTP credentials not configured.');
        return;
    }

    const mailOptions = {
        from: `"MSS Licensing" <${process.env.SMTP_USER}>`,
        to: to,
        subject: 'Your IMS License is Active!',
        html: `
            <div style="font-family: sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;">
                <h2 style="color: #007ACC;">License Activated Successfully</h2>
                <p>Hello <strong>${username}</strong>,</p>
                <p>Your payment has been verified and your <strong>${tier}</strong> license for IMS (Inventory Management System) is now active.</p>
                
                <div style="background: #f9f9f9; padding: 15px; border-radius: 8px; margin: 20px 0;">
                    <p style="margin: 0; font-size: 0.8rem; color: #666; text-transform: uppercase; font-weight: bold;">Your Signed License Key:</p>
                    <code style="display: block; background: #fff; padding: 10px; border: 1px solid #ddd; border-radius: 4px; word-break: break-all; margin-top: 5px; color: #007ACC; font-weight: bold;">
                        ${licenseKey}
                    </code>
                </div>

                <p>Copy this key and paste it into the <strong>License</strong> section of your IMS Desktop application to unlock your features.</p>
                
                <hr style="border: 0; border-top: 1px solid #eee; margin: 30px 0;" />
                <p style="font-size: 0.8rem; color: #999;">If you didn't request this, please ignore this email.</p>
                <p style="font-size: 0.8rem; color: #999;">Support: mwimulebienvenu05@gmail.com</p>
            </div>
        `,
    };

    try {
        await transporter.sendMail(mailOptions);
        console.log(`Activation email sent to ${to}`);
    } catch (error) {
        console.error('Error sending email:', error);
    }
}
