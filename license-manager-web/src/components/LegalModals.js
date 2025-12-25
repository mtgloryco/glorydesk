'use client';

import { X, Shield, FileText } from 'lucide-react';

export function PrivacyModal({ isOpen, onClose }) {
    if (!isOpen) return null;

    return (
        <div className="legal-modal-overlay">
            <div className="legal-modal-content">
                <button onClick={onClose} className="close-btn"><X size={24} /></button>
                <div className="legal-header">
                    <Shield size={32} className="icon" />
                    <h2>Data Privacy Policy</h2>
                    <p>Last updated: December 25, 2025</p>
                </div>

                <div className="legal-body">
                    <section>
                        <h3>1. Introduction</h3>
                        <p>IMS Manager ("We", "Us", "Our") is committed to your privacy and the security of your data. This Privacy Policy describes our policies on the collection, use, and disclosure of your information when you use the IMS Platform.</p>
                        <p>By using the Platform, you agree to the collection and use of information in accordance with this Privacy Policy.</p>
                    </section>

                    <section>
                        <h3>2. Data Collection & Usage</h3>
                        <p><strong>Types of Data Collected:</strong> We collect personal data necessary for license activation and support, including:</p>
                        <ul>
                            <li>Email address (for account management)</li>
                            <li>Full Name / Business Name</li>
                            <li>Hardware ID (for license binding)</li>
                            <li>Usage Data (login timestamps, license validation status)</li>
                        </ul>
                        <p><strong>Purpose:</strong> We use this data to provide the Service, manage your Account, perform contract obligations (License issuance), and contact you regarding updates or support.</p>
                    </section>

                    <section>
                        <h3>3. Data Protection Rights (Rwanda Law No 058/21021)</h3>
                        <p>In compliance with the <strong>Law No 058/21021 of 13/10/2021</strong> relating to the protection of personal data and privacy, you are entitled to the following rights:</p>
                        <ul>
                            <li><strong>Right to personal data:</strong> The data belongs to You.</li>
                            <li><strong>Right to object:</strong> You have the right to request us to stop processing your personal data.</li>
                            <li><strong>Right to rectification:</strong> You have the right to correct inaccurate or incomplete information.</li>
                            <li><strong>Right to erasure:</strong> You have the right to request deletion of your data (subject to legal retention requirements).</li>
                            <li><strong>Right to data portability:</strong> You can request your data be transferred to you or another organization.</li>
                            <li><strong>Right to restriction:</strong> You can restrict processing under certain conditions.</li>
                        </ul>
                    </section>

                    <section>
                        <h3>4. Security</h3>
                        <p>The security of your Personal Data is important to us. We use commercially acceptable means (encryption, secure access controls) to protect your Personal Data, but remember that no method of transmission over the Internet is 100% secure.</p>
                    </section>

                    <section>
                        <h3>5. Contact Us</h3>
                        <p>If you have questions about this Privacy Policy or wish to exercise your rights, please contact our Data Protection Officer:</p>
                        <p><strong>Email:</strong> mwimulebienvenu05@gmail.com</p>
                        <p><strong>Location:</strong> Kigali, Rwanda</p>
                    </section>
                </div>
            </div>
            <style jsx>{`
                .legal-modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.6); backdrop-filter: blur(5px); z-index: 3000; display: flex; align-items: center; justify-content: center; padding: 1rem; }
                .legal-modal-content { background: #fff; width: 100%; max-width: 700px; height: 85vh; border-radius: 20px; display: flex; flex-direction: column; overflow: hidden; box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25); position: relative; }
                .close-btn { position: absolute; top: 15px; right: 15px; background: #f1f5f9; border: none; padding: 8px; border-radius: 50%; cursor: pointer; transition: background 0.2s; }
                .close-btn:hover { background: #e2e8f0; }
                
                .legal-header { padding: 2rem; background: #f8fafc; border-bottom: 1px solid #e2e8f0; text-align: center; }
                .legal-header h2 { font-size: 1.8rem; font-weight: 800; color: #0f172a; margin: 0.5rem 0; }
                .legal-header p { color: #64748b; font-size: 0.9rem; }
                .icon { color: #3b82f6; }
                
                .legal-body { padding: 2rem; overflow-y: auto; color: #334155; line-height: 1.7; font-size: 0.95rem; }
                .legal-body section { margin-bottom: 2rem; }
                .legal-body h3 { font-size: 1.1rem; font-weight: 700; color: #0f172a; margin-bottom: 0.8rem; }
                .legal-body ul { padding-left: 1.5rem; margin-bottom: 1rem; }
                .legal-body li { margin-bottom: 0.5rem; }
            `}</style>
        </div>
    );
}

export function TermsModal({ isOpen, onClose }) {
    if (!isOpen) return null;

    return (
        <div className="legal-modal-overlay">
            <div className="legal-modal-content">
                <button onClick={onClose} className="close-btn"><X size={24} /></button>
                <div className="legal-header">
                    <FileText size={32} className="icon" style={{ color: '#8b5cf6' }} />
                    <h2>Terms of Service</h2>
                    <p>Effective Date: December 25, 2025</p>
                </div>

                <div className="legal-body">
                    <section>
                        <h3>1. Agreement to Terms</h3>
                        <p>These Terms of Use constitute a legally binding agreement made between you, whether personally or on behalf of an entity ("You") and IMS Manager ("Company"), concerning your access to and use of the IMS desktop application and website.</p>
                    </section>

                    <section>
                        <h3>2. License Grant</h3>
                        <p>The Company grants you a revocable, non-exclusive, non-transferable, limited license to download, install, and use the Application strictly in accordance with the terms of this Agreement.</p>
                        <p><strong>License Restrictions:</strong> You shall not: (1) decompile, reverse engineer, disassemble, or derive the source code of the App; (2) use the App for any revenue generating endeavor other than the agreed internal business inventory management; (3) viololate any applicable laws, including Rwanda laws regarding data security.</p>
                    </section>

                    <section>
                        <h3>3. User Representations</h3>
                        <p>By using the Application, you represent and warrant that: (1) all registration information you submit will be true, accurate, current, and complete; (2) you will maintain the accuracy of such information; (3) you have the legal capacity and you agree to comply with these Terms of Use.</p>
                    </section>

                    <section>
                        <h3>4. Disclaimer</h3>
                        <p>The Application is provided on an AS-IS and AS-AVAILABLE basis. You agree that your use of the Application and our services will be at your sole risk. To the fullest extent permitted by law, we disclaim all warranties, express or implied, in connection with the Application and your use thereof.</p>
                    </section>

                    <section>
                        <h3>5. Governing Law</h3>
                        <p>These Terms shall be governed by and defined following the laws of <strong>Rwanda</strong>. IMS Manager and yourself irrevocably consent that the courts of Rwanda shall have exclusive jurisdiction to resolve any dispute which may arise in connection with these terms.</p>
                    </section>

                    <section>
                        <h3>6. Contact</h3>
                        <p>For any questions regarding these Terms, please contact us at:</p>
                        <p><strong>Email:</strong> mwimulebienvenu05@gmail.com</p>
                    </section>
                </div>
            </div>
            <style jsx>{`
                .legal-modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.6); backdrop-filter: blur(5px); z-index: 3000; display: flex; align-items: center; justify-content: center; padding: 1rem; }
                .legal-modal-content { background: #fff; width: 100%; max-width: 700px; height: 85vh; border-radius: 20px; display: flex; flex-direction: column; overflow: hidden; box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25); position: relative; }
                .close-btn { position: absolute; top: 15px; right: 15px; background: #f1f5f9; border: none; padding: 8px; border-radius: 50%; cursor: pointer; transition: background 0.2s; }
                .close-btn:hover { background: #e2e8f0; }
                
                .legal-header { padding: 2rem; background: #f8fafc; border-bottom: 1px solid #e2e8f0; text-align: center; }
                .legal-header h2 { font-size: 1.8rem; font-weight: 800; color: #0f172a; margin: 0.5rem 0; }
                .legal-header p { color: #64748b; font-size: 0.9rem; }
                .icon { color: #3b82f6; }
                
                .legal-body { padding: 2rem; overflow-y: auto; color: #334155; line-height: 1.7; font-size: 0.95rem; }
                .legal-body section { margin-bottom: 2rem; }
                .legal-body h3 { font-size: 1.1rem; font-weight: 700; color: #0f172a; margin-bottom: 0.8rem; }
                .legal-body ul { padding-left: 1.5rem; margin-bottom: 1rem; }
                .legal-body li { margin-bottom: 0.5rem; }
            `}</style>
        </div>
    );
}
