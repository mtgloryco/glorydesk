import { NextResponse } from 'next/server';
import clientPromise from '@/lib/mongodb';
import { ObjectId } from 'mongodb';

const DB_NAME = 'license-manager';
const COLLECTION = 'downloads';

// Initial seed data if DB is empty
const SEED_DOWNLOADS = [
    {
        version: '1.2.0',
        os: 'Windows',
        type: 'Installer',
        link: 'https://example.com/download/windows-latest.exe',
        description: 'Latest stable release for Windows. Includes extensive bug fixes and performance improvements.',
        releaseDate: new Date().toISOString(),
        isFeatured: true
    },
    {
        version: '1.2.0',
        os: 'Linux',
        type: 'Archive',
        link: 'https://example.com/download/linux-latest.tar.gz',
        description: 'Latest stable release for Linux (Debian/Ubuntu/Fedora).',
        releaseDate: new Date().toISOString(),
        isFeatured: true
    },
    {
        version: '1.1.0',
        os: 'Windows',
        type: 'Installer',
        link: 'https://example.com/download/windows-v1.1.exe',
        description: 'Legacy version 1.1.0.',
        releaseDate: new Date(Date.now() - 86400000 * 30).toISOString(),
        isFeatured: false
    }
];

export async function GET(request) {
    try {
        const client = await clientPromise;
        const db = client.db(DB_NAME);
        const url = new URL(request.url);
        const featured = url.searchParams.get('featured');

        // Auto-seed if empty
        const count = await db.collection(COLLECTION).countDocuments();
        if (count === 0) {
            await db.collection(COLLECTION).insertMany(SEED_DOWNLOADS);
        }

        let query = {};
        if (featured === 'true') {
            query.isFeatured = true;
        }

        const downloads = await db.collection(COLLECTION)
            .find(query)
            .sort({ releaseDate: -1 })
            .toArray();

        return NextResponse.json(downloads);
    } catch (e) {
        console.error('GET /api/downloads error:', e);
        return NextResponse.json({ error: e.message }, { status: 500 });
    }
}

export async function POST(request) {
    try {
        const client = await clientPromise;
        const db = client.db(DB_NAME);
        const body = await request.json(); // Expect: { version, os, type, link, description, releaseDate, isFeatured }

        if (!body.version || !body.link || !body.os) {
            return NextResponse.json({ error: 'Missing required fields' }, { status: 400 });
        }

        // Validate duplicates
        const existing = await db.collection(COLLECTION).findOne({ version: body.version, os: body.os });
        if (existing) {
            return NextResponse.json({ error: 'Version already exists for this OS' }, { status: 400 });
        }

        // If featured, unfeature others of same OS? (Optional, let's allow multiple featured for now, or user manages it manually)
        // User requested: "on the existing section we can show only the verison that admnin seted as featured.."
        // Let's stick to manual control for flexibility unless strict enforcement needed.

        const newDownload = {
            ...body,
            releaseDate: body.releaseDate || new Date().toISOString(),
            isFeatured: body.isFeatured || false,
            createdAt: new Date()
        };

        await db.collection(COLLECTION).insertOne(newDownload);
        return NextResponse.json({ success: true });
    } catch (e) {
        return NextResponse.json({ error: e.message }, { status: 500 });
    }
}

export async function PUT(request) {
    try {
        const client = await clientPromise;
        const db = client.db(DB_NAME);
        const body = await request.json();
        const { id, ...updateData } = body;

        if (!id) return NextResponse.json({ error: 'ID required' }, { status: 400 });

        // If we want to strictly toggle featured, update logic here.
        // For now, straight update.

        delete updateData._id; // Prevent _id mutation

        await db.collection(COLLECTION).updateOne(
            { _id: new ObjectId(id) },
            { $set: updateData }
        );

        return NextResponse.json({ success: true });
    } catch (e) {
        return NextResponse.json({ error: e.message }, { status: 500 });
    }
}

export async function DELETE(request) {
    try {
        const client = await clientPromise;
        const db = client.db(DB_NAME);
        const { searchParams } = new URL(request.url);
        const id = searchParams.get('id');

        if (!id) return NextResponse.json({ error: 'ID required' }, { status: 400 });

        await db.collection(COLLECTION).deleteOne({ _id: new ObjectId(id) });
        return NextResponse.json({ success: true });
    } catch (e) {
        return NextResponse.json({ error: e.message }, { status: 500 });
    }
}
