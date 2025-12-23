import Link from 'next/link';

export default function TermsPage() {
    return (
        <div className="container" style={{ maxWidth: '800px', padding: '4rem 2rem' }}>
            <Link href="/" style={{ color: 'var(--primary)', textDecoration: 'none', marginBottom: '2rem', display: 'inline-block' }}>
                ← Back to Home
            </Link>
            <h1 className="title" style={{ fontSize: '2.5rem', marginBottom: '2rem' }}>License Terms</h1>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>1. Grant of License</h2>
                <p>
                    By purchasing a premium license, IMS grants you a non-exclusive, non-transferable right to use the software on a single device bound by the Hardware ID provided at the time of issuance.
                </p>
            </section>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>2. Offline Activation</h2>
                <p>
                    Licenses are cryptographically signed and validated offline. You do not need a constant internet connection to use the software after activation. However, the license must be renewed through the web portal once it expires.
                </p>
            </section>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>3. Restrictions</h2>
                <p>
                    You may not:
                </p>
                <ul style={{ marginLeft: '1.5rem', marginTop: '1rem' }}>
                    <li>Resell, distribute, or sub-license the software.</li>
                    <li>Reverse engineer or attempt to forge signed licenses.</li>
                    <li>Attempt to bypass hardware binding restrictions.</li>
                </ul>
            </section>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>4. Expiration & Renewal</h2>
                <p>
                    Upon expiration of a premium license, the software will automatically revert to the "Free" tier. All data remains safe, but premium features (like cloud backup or bulk import) will be disabled until a new license is activated.
                </p>
            </section>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>5. Liability</h2>
                <p>
                    The software is provided "as is". We are not responsible for any data loss resulting from hardware failure or misuse of the software. Users are encouraged to perform regular local backups.
                </p>
            </section>

            <footer style={{ marginTop: '4rem', paddingTop: '2rem', borderTop: '1px solid #eee', textAlign: 'center', color: '#666' }}>
                &copy; {new Date().getFullYear()} IMS License Manager. All rights reserved.
            </footer>
        </div>
    );
}
