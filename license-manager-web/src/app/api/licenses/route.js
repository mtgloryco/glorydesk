import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';
import { getAuthUser } from '@/lib/auth';
import { v4 as uuidv4 } from 'uuid';
import { LICENSE_CONFIG } from '@/lib/config';

export async function GET(request) {
    const user = await getAuthUser(request);
    if (!user) return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });

    const client = await clientPromise;
    const db = client.db('license_manager');

    const licenses = await db.collection('licenses')
        .find({ userId: user.userId })
        .sort({ createdAt: -1 })
        .toArray();

    return NextResponse.json(licenses);
}

// Request License (Sets to Pending)
export async function POST(request) {
    const user = await getAuthUser(request);
    if (!user) return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });

    try {
        const { planType, hardwareId } = await request.json();

        const plan = LICENSE_CONFIG.plans[planType];
        if (!plan) {
            return NextResponse.json({ error: 'Invalid plan selected.' }, { status: 400 });
        }

        if (!hardwareId) {
            return NextResponse.json({ error: 'Hardware ID is required.' }, { status: 400 });
        }

        const client = await clientPromise;
        const db = client.db('license_manager');

        // Rate Limiting (count both Active and Pending requests)
        const activeCount = await db.collection('licenses').countDocuments({
            userId: user.userId,
            status: { $in: ['Active', 'Pending'] }
        });

        if (activeCount >= LICENSE_CONFIG.limits.maxActivePerUser) {
            return NextResponse.json({ error: 'Limit reached. Please contact support.' }, { status: 429 });
        }

        const licenseId = uuidv4();

        const newLicense = {
            licenseId,
            userId: user.userId,
            hardwareId,
            tier: plan.tier,
            planType: planType,
            status: 'Pending', // Wait for admin activation
            paymentProof: null, // Will be uploaded later
            issuedAt: null,
            expirationDate: null,
            licenseKey: null, // Generated during approval
            createdAt: new Date(),
            updatedAt: new Date()
        };

        await db.collection('licenses').insertOne(newLicense);

        return NextResponse.json(newLicense, { status: 201 });
    } catch (error) {
        console.error('License Request Error:', error);
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}

// Upload Payment Proof
export async function PATCH(request) {
    const user = await getAuthUser(request);
    if (!user) return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });

    try {
        const { id, proofImage } = await request.json(); // id is the MongoDB _id

        if (!proofImage) {
            return NextResponse.json({ error: 'Proof image is required.' }, { status: 400 });
        }

        const client = await clientPromise;
        const db = client.db('license_manager');
        const { ObjectId } = await import('mongodb');

        const result = await db.collection('licenses').updateOne(
            { _id: new ObjectId(id), userId: user.userId },
            {
                $set: {
                    paymentProof: proofImage,
                    updatedAt: new Date()
                }
            }
        );

        if (result.matchedCount === 0) {
            return NextResponse.json({ error: 'License request not found.' }, { status: 404 });
        }

        return NextResponse.json({ success: true });
    } catch (error) {
        return NextResponse.json({ error: error.message }, { status: 500 });
    }
}
