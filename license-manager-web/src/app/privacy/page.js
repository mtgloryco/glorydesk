'use client';

import Link from 'next/link';
import { LEGAL_CONTENT } from '@/lib/legalContent';

export default function PrivacyPage() {
    const { title, lastUpdated, sections } = LEGAL_CONTENT.privacy;

    return (
        <div className="container" style={{ maxWidth: '800px', padding: '4rem 2rem' }}>
            <Link href="/" style={{ color: 'var(--primary)', textDecoration: 'none', marginBottom: '2rem', display: 'inline-block' }}>
                ← Back to Home
            </Link>
            <h1 className="title" style={{ fontSize: '2.5rem', marginBottom: '0.5rem' }}>{title}</h1>
            <p style={{ marginBottom: '3rem', color: '#666' }}>Last updated: {lastUpdated}</p>

            {sections.map((section, index) => (
                <section key={index} style={{ marginBottom: '2rem' }}>
                    <h2 style={{ marginBottom: '1rem', fontSize: '1.2rem' }}>{section.title}</h2>
                    <div className="legal-content">
                        {section.content}
                    </div>
                </section>
            ))}

            <footer style={{ marginTop: '4rem', paddingTop: '2rem', borderTop: '1px solid #eee', textAlign: 'center', color: '#666' }}>
                &copy; {new Date().getFullYear()} IMS License Manager. All rights reserved.
            </footer>

            <style jsx global>{`
                .legal-content p { margin-bottom: 1rem; line-height: 1.6; color: #444; }
                .legal-content ul { margin-left: 1.5rem; margin-bottom: 1rem; color: #444; }
                .legal-content li { margin-bottom: 0.5rem; }
            `}</style>
        </div>
    );
}


