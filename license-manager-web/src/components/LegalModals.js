'use client';

import { X, Shield, FileText } from 'lucide-react';
import { LEGAL_CONTENT } from '@/lib/legalContent';

export function PrivacyModal({ isOpen, onClose }) {
    if (!isOpen) return null;

    const { title, lastUpdated, sections } = LEGAL_CONTENT.privacy;

    return (
        <div className="legal-modal-overlay">
            <div className="legal-modal-content">
                <button onClick={onClose} className="close-btn"><X size={24} /></button>
                <div className="legal-header">
                    <Shield size={32} className="icon" />
                    <h2>{title}</h2>
                    <p>Last updated: {lastUpdated}</p>
                </div>

                <div className="legal-body">
                    {sections.map((section, index) => (
                        <section key={index}>
                            <h3>{section.title}</h3>
                            {section.content}
                        </section>
                    ))}
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

    const { title, effectiveDate, sections } = LEGAL_CONTENT.terms;

    return (
        <div className="legal-modal-overlay">
            <div className="legal-modal-content">
                <button onClick={onClose} className="close-btn"><X size={24} /></button>
                <div className="legal-header">
                    <FileText size={32} className="icon" style={{ color: '#8b5cf6' }} />
                    <h2>{title}</h2>
                    <p>Effective Date: {effectiveDate}</p>
                </div>

                <div className="legal-body">
                    {sections.map((section, index) => (
                        <section key={index}>
                            <h3>{section.title}</h3>
                            {section.content}
                        </section>
                    ))}
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
