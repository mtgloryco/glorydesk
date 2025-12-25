import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';
import nodemailer from 'nodemailer';
import { ObjectId } from 'mongodb';

export async function POST(request) {
    try {
        const { name, email, message } = await request.json();

        if (!email || !message) {
            return NextResponse.json({ error: 'Email and message are required.' }, { status: 400 });
        }

        const client = await clientPromise;
        const db = client.db('license_manager');

        const newContact = {
            name,
            email,
            message,
            status: 'New',
            createdAt: new Date()
        };

        await db.collection('contacts').insertOne(newContact);

        return NextResponse.json({ success: true, message: 'Message sent successfully.' }, { status: 201 });
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}

export async function GET(request) {
    try {
        const client = await clientPromise;
        const db = client.db('license_manager');
        const contacts = await db.collection('contacts').find({}).sort({ createdAt: -1 }).toArray();
        return NextResponse.json(contacts);
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}

// PATCH: Reply to message
export async function PATCH(request) {
    try {
        const { id, replyMessage } = await request.json();

        if (!id || !replyMessage) {
            return NextResponse.json({ error: 'ID and Reply Message are required.' }, { status: 400 });
        }

        const client = await clientPromise;
        const db = client.db('license_manager');

        // Update status in DB
        const result = await db.collection('contacts').updateOne(
            { _id: new ObjectId(id) },
            {
                $set: { status: 'Replied', repliedAt: new Date() },
                $push: { replies: { message: replyMessage, sentAt: new Date() } }
            }
        );

        if (result.matchedCount === 0) return NextResponse.json({ error: 'Message not found' }, { status: 404 });

        // Get the contact to send email
        const contact = await db.collection('contacts').findOne({ _id: new ObjectId(id) });

        // Send Email via Nodemailer
        // NOTE: You need to configure these env variables: SMTP_HOST, SMTP_USER, SMTP_PASS
        if (process.env.SMTP_HOST && process.env.SMTP_USER) {
            const transporter = nodemailer.createTransport({
                host: process.env.SMTP_HOST,
                port: 587,
                secure: false,
                auth: {
                    user: process.env.SMTP_USER,
                    pass: process.env.SMTP_PASS,
                },
            });

            await transporter.sendMail({
                from: `"Support Team" <${process.env.SMTP_USER}>`,
                to: contact.email,
                subject: `Re: Your message to IMS`,
                text: replyMessage,
                html: `<p>Hello ${contact.name},</p><p>${replyMessage}</p><br/><p>Best regards,<br/>IMS Support Team</p>`
            });
        } else {
            console.warn('SMTP not configured, email NOT sent, but status updated.');
        }

        return NextResponse.json({ success: true });
    } catch (error) {
        console.error('Reply failed:', error);
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
