import React, { useEffect } from "react";
import useDocusaurusContext from "@docusaurus/useDocusaurusContext";
import Layout from "@theme/Layout";

export default function Home() {
    const { siteConfig } = useDocusaurusContext();

    useEffect(() => {
        // Client-side redirect to the docs intro page
        const target = `${siteConfig.baseUrl || "/"}docs/intro`;
        // Use replace so the redirect doesn't create a new history entry
        window.location.replace(target);
    }, [siteConfig.baseUrl]);

    // Fallback content for non-JS clients and crawlers
    return (
        <Layout
            title={`${siteConfig.title} — Stella Wiki`}
            description="A developer wiki for Astra Aeterna: design notes, engine documentation, and lore."
        >
            <main style={{ padding: "4rem 0", textAlign: "center" }}>
                <h1>{siteConfig.title}</h1>
                <p>Redirecting to the docs…</p>
                <p>
                    If you are not redirected automatically,{" "}
                    <a href="/docs/intro">click here</a>.
                </p>
            </main>
        </Layout>
    );
}
