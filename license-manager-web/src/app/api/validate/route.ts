import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';

export async function POST(request) {
    try {
        const { licenseKey, deviceFingerprint } = await request.json();

        if (!licenseKey) {
            return NextResponse.json({ valid: false, message: 'Missing license key' }, { status: 400 });
        }

        const client = await clientPromise;
        const db = client.db('license_manager');

        const license = await db.collection('licenses').findOne({ licenseKey });

        if (!license) {
            return NextResponse.json({ valid: false, message: 'License not found' }, { status: 404 });
        }

        if (license.status !== 'Active') {
            return NextResponse.json({ valid: false, message: `License is ${license.status}` }, { status: 403 });
        }

        const now = new Date();
        if (now > new Date(license.expirationDate)) {
            // Update status to expired
            await db.collection('licenses').updateOne(
                { _id: license._id },
                { $set: { status: 'Expired', updatedAt: now } }
            );
            return NextResponse.json({ valid: false, message: 'License expired' }, { status: 403 });
        }

        // Fingerprint check (optional logic: bind on first use)
        if (license.deviceFingerprint && license.deviceFingerprint !== deviceFingerprint) {
            return NextResponse.json({ valid: false, message: 'Hardware mismatch' }, { status: 403 });
        }

        if (!license.deviceFingerprint && deviceFingerprint) {
            await db.collection('licenses').updateOne(
                { _id: license._id },
                { $set: { deviceFingerprint, updatedAt: now } }
            );
        }

        return NextResponse.json({
            valid: true,
            planType: license.planType,
            expirationDate: license.expirationDate
        });
    } catch (error) {
        return NextResponse.json({ valid: false, message: error.message }, { status: 500 });
    }
}
