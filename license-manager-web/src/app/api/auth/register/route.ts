import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';
import { hashPassword } from '@/lib/auth';

export async function POST(request) {
    try {
        const { email, password, username } = await request.json();

        if (!email || !password) {
            return NextResponse.json({ error: 'Missing fields' }, { status: 400 });
        }

        const client = await clientPromise;
        const db = client.db('license_manager');

        // Check existing
        const existing = await db.collection('users').findOne({ email: email.toLowerCase() });
        if (existing) {
            return NextResponse.json({ error: 'User already exists' }, { status: 400 });
        }

        const hashedPassword = await hashPassword(password);

        const newUser = {
            email: email.toLowerCase(),
            username: username || email.split('@')[0],
            password: hashedPassword,
            role: 'user',
            createdAt: new Date()
        };

        await db.collection('users').insertOne(newUser);

        return NextResponse.json({ message: 'User created successfully' }, { status: 201 });
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
