import Link from 'next/link';

export default function PrivacyPage() {
    return (
        <div className="container" style={{ maxWidth: '800px', padding: '4rem 2rem' }}>
            <Link href="/" style={{ color: 'var(--primary)', textDecoration: 'none', marginBottom: '2rem', display: 'inline-block' }}>
                ← Back to Home
            </Link>
            <h1 className="title" style={{ fontSize: '2.5rem', marginBottom: '2rem' }}>Privacy Policy</h1>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>1. Information We Collect</h2>
                <p>
                    We value your privacy. The Inventory Management System (IMS) and its license manager collect only the minimum data necessary to function:
                </p>
                <ul style={{ marginLeft: '1.5rem', marginTop: '1rem' }}>
                    <li><strong>Account Data:</strong> Email address and encrypted password for authentication.</li>
                    <li><strong>Hardware Identifier:</strong> A non-reversible hash of your device's hardware (CPU, Disk, Machine ID) to bind licenses to specific machines.</li>
                    <li><strong>License Metadata:</strong> Tier, issue date, and expiration date.</li>
                </ul>
            </section>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>2. How We Use Data</h2>
                <p>
                    Your email is used for account recovery and identification. The hardware identifier is used strictly for offline license validation and to prevent unauthorized distribution of premium keys.
                    <strong> We do not collect any inventory data, financial records, or customer lists from your desktop application.</strong>
                </p>
            </section>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>3. Data Storage</h2>
                <p>
                    All sensitive inventory data is stored locally on your machine in a SQLite database. Our web servers only store license-related information.
                </p>
            </section>

            <section style={{ marginBottom: '2rem' }}>
                <h2 style={{ marginBottom: '1rem' }}>4. Contact Us</h2>
                <p>
                    If you have any questions about this policy, contact us at support@inventorysystem.local.
                </p>
            </section>

            <footer style={{ marginTop: '4rem', paddingTop: '2rem', borderTop: '1px solid #eee', textAlign: 'center', color: '#666' }}>
                &copy; {new Date().getFullYear()} IMS License Manager. All rights reserved.
            </footer>
        </div>
    );
}
