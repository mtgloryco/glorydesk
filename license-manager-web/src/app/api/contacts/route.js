import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';

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
    // Admin check logic could be added here
    try {
        const client = await clientPromise;
        const db = client.db('license_manager');
        const contacts = await db.collection('contacts').find({}).sort({ createdAt: -1 }).toArray();
        return NextResponse.json(contacts);
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
