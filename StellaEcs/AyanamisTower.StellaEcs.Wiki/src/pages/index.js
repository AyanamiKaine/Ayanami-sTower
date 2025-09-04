import clsx from "clsx";
import Link from "@docusaurus/Link";
import useDocusaurusContext from "@docusaurus/useDocusaurusContext";
import Layout from "@theme/Layout";
import HomepageFeatures from "@site/src/components/HomepageFeatures";

import Heading from "@theme/Heading";
import styles from "./index.module.css";

function HomepageHeader() {
    const { siteConfig } = useDocusaurusContext();
    return (
        <header className={clsx("hero hero--primary", styles.heroBanner)}>
            <div className="container">
                <Heading as="h1" className="hero__title">
                    {siteConfig.title}
                </Heading>
                <p className="hero__subtitle">{siteConfig.tagline}</p>
                <p className="hero__subtitle">
                    A compact, developer-focused wiki for Astra Aeterna: game
                    design notes, engine internals, and the project's lore.
                </p>

                <div className={styles.buttons}>
                    <Link
                        className="button button--secondary button--lg"
                        to="/docs/intro"
                    >
                        Read the Intro
                    </Link>
                </div>

                <div className={styles.buttons}>
                    <Link
                        className="button button--outline button--lg"
                        to="/docs/Design/overview"
                    >
                        Design
                    </Link>
                    <Link
                        className="button button--outline button--lg"
                        to="/docs/engine"
                    >
                        Engine
                    </Link>
                    <Link
                        className="button button--outline button--lg"
                        to="/docs/lore"
                    >
                        Lore
                    </Link>
                </div>
            </div>
        </header>
    );
}

export default function Home() {
    const { siteConfig } = useDocusaurusContext();
    return (
        <Layout
            title={`${siteConfig.title} — Stella Wiki`}
            description="A developer wiki for Astra Aeterna: design notes, engine documentation, and lore."
        >
            <HomepageHeader />
            <main>
                <HomepageFeatures />
            </main>
        </Layout>
    );
}
