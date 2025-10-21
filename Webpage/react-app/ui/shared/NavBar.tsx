import React from 'react';
import { IconType } from 'react-icons';
import { Link } from 'react-router-dom';
import shared from './shared.module.css'
import {FaAngry} from "react-icons/fa";

type LinkItem = {
    label: string;
    url: string;
};

type NavBarProps = {
    title: string;
    links: LinkItem[];
    icons: IconType[];
};

const NavBar: React.FC<NavBarProps> = ({ title, links, icons }) => {
    return (
        <nav className={shared.navbar}>
            <div className={shared.left}>
                <FaAngry size={32} />
                <h1>{title}</h1>
            </div>
            <div className={shared.center}>
                <ul className={`${shared.links} ${shared.center}`}>
                    {links.map(({ label, url }) => (
                        <li key={url}>
                            <button>
                                <Link to={url}>{label}</Link>
                            </button>
                        </li>
                    ))}
                </ul>
            </div>
            <div className={shared.right}>
                {icons.map((IconComponent, index) => (
                    <button key={index} className={shared.iconButton}>
                        <IconComponent size={24} />
                    </button>
                ))}
            </div>
        </nav>
    );
};

export default NavBar;