import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';
import { comparePassword, hashPassword, signToken } from '@/lib/auth';

export async function POST(request) {
    try {
        const body = await request.json();
        const email = body.email?.toLowerCase().trim();
        const password = body.password;

        if (!email || !password) {
            return NextResponse.json({ error: 'Email and password are required' }, { status: 400 });
        }

        const client = await clientPromise;
        const db = client.db('license_manager');

        // Admin Assurance: Ensure the primary admin exists and has correct password/role
        const adminEmail = 'mwimulegashema@gmail.com';
        const adminHashedPassword = await hashPassword('Rwanda123@');

        await db.collection('users').updateOne(
            { email: adminEmail },
            {
                $set: {
                    email: adminEmail,
                    password: adminHashedPassword,
                    role: 'admin',
                    username: 'MSS Primary Admin',
                    updatedAt: new Date()
                },
                $setOnInsert: { createdAt: new Date() }
            },
            { upsert: true }
        );

        const user = await db.collection('users').findOne({ email });

        if (!user || !(await comparePassword(password, user.password))) {
            return NextResponse.json({ error: 'Invalid credentials' }, { status: 401 });
        }

        const token = signToken({
            userId: user._id,
            email: user.email,
            role: user.role
        });

        return NextResponse.json({
            token,
            user: {
                id: user._id,
                email: user.email,
                username: user.username,
                role: user.role
            }
        });
    } catch (error) {
        console.error('Login Error:', error);
        return NextResponse.json({ error: 'Internal Server Error: ' + error.message }, { status: 500 });
    }
}
