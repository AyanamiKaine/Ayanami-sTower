import clsx from "clsx";
import Link from "@docusaurus/Link";
import Heading from "@theme/Heading";
import styles from "./styles.module.css";

const FeatureList = [
    {
        title: "Design",
        Svg: require("@site/static/img/icon-design.svg").default,
        to: "/docs/Design/overview",
        description: (
            <>
                Notes and essays on game design decisions for Astra Aeterna.
                Deep dives into difficulty, mechanics, and how to make
                large-scale strategy games remain engaging.
            </>
        ),
    },
    {
        title: "Engine",
        Svg: require("@site/static/img/icon-engine.svg").default,
        to: "/docs/engine",
        description: (
            <>
                Engine internals, modding guides, and technical notes — useful
                for contributors and anyone extending the project.
            </>
        ),
    },
    {
        title: "Lore",
        Svg: require("@site/static/img/icon-lore.svg").default,
        to: "/docs/lore",
        description: (
            <>
                The worldbuilding and story background for Astra Aeterna:
                factions, history, and flavour that give the project its
                identity.
            </>
        ),
    },
];

function Feature({ Svg, title, description, to }) {
    return (
        <div className={clsx("col col--4")}>
            <div className="text--center">
                <Svg className={styles.featureSvg} role="img" />
            </div>
            <div className="text--center padding-horiz--md">
                <Heading as="h3">{title}</Heading>
                <p>{description}</p>
                {to && (
                    <div style={{ marginTop: "0.5rem" }}>
                        <Link
                            className="button button--outline button--sm"
                            to={to}
                        >
                            Learn more
                        </Link>
                    </div>
                )}
            </div>
        </div>
    );
}

export default function HomepageFeatures() {
    return (
        <section className={styles.features}>
            <div className="container">
                <div className="row">
                    {FeatureList.map((props, idx) => (
                        <Feature key={idx} {...props} />
                    ))}
                </div>
            </div>
        </section>
    );
}
